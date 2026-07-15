using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CV.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace CV.Api.Diagnostics;

/// <summary>
/// Read-only troubleshooting endpoints for the deployed site.
///
///  GET /api/health — ultra-light liveness probe. 200 means the app process is
///                    up and serving requests. Safe to hit from uptime monitors.
///
///  GET /api/diag   — a full, secret-free deployment report: environment, config
///                    presence (booleans only — never the values), database
///                    connectivity + record counts, startup outcome, PDF/Chrome
///                    availability, and runtime info. This is what tells you *why*
///                    a fresh deploy is "not working" without RDP-ing into the host.
///
/// Deliberately unauthenticated: it exposes no secrets, and the admin key (which
/// gates the write endpoint) is often the very thing that's misconfigured — so
/// gating diagnostics behind it would defeat the purpose. If you later want it
/// locked down, add .AddEndpointFilter&lt;AdminKeyFilter&gt;() to the /diag map.
/// </summary>
public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnostics(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            timeUtc = DateTime.UtcNow,
        }));

        routes.MapGet("/diag", async (IServiceProvider sp, IConfiguration config, IHostEnvironment env, CancellationToken ct) =>
        {
            var contentRoot = env.ContentRootPath;

            object database;
            try
            {
                // Resolve the DbContext HERE, inside the try, rather than via handler
                // injection — so a construction failure (e.g. a corrupt/missing SQL
                // client assembly after a bad deploy, or an invalid connection
                // string) is reported instead of 500ing this endpoint too.
                var db = sp.GetRequiredService<CvDbContext>();

                var canConnect = await db.Database.CanConnectAsync(ct);
                int? profiles = null, skills = null, jobs = null, education = null;
                string[] pendingMigrations = [];

                if (canConnect)
                {
                    profiles = await db.Profiles.CountAsync(ct);
                    skills = await db.Skills.CountAsync(ct);
                    jobs = await db.Jobs.CountAsync(ct);
                    education = await db.Education.CountAsync(ct);
                    if (db.Database.IsRelational())
                        pendingMigrations = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
                }

                database = new
                {
                    provider = db.Database.ProviderName,
                    isRelational = db.Database.IsRelational(),
                    canConnect,
                    hasData = profiles is > 0,
                    pendingMigrations,
                    counts = new { profiles, skills, jobs, education },
                };
            }
            catch (Exception ex)
            {
                // Flatten the chain so the real cause (the inner exception) is visible
                // — that's where "Could not load file or assembly ...SqlClient..." or a
                // connection/model-building failure actually shows up.
                var chain = new List<string>();
                for (Exception? e = ex; e is not null; e = e.InnerException)
                    chain.Add($"{e.GetType().Name}: {e.Message}");

                database = new
                {
                    canConnect = false,
                    error = string.Join(" || ", chain),
                };
            }

            DateTime? startedUtc = null;
            long? uptimeSeconds = null;
            try
            {
                startedUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();
                uptimeSeconds = (long)(DateTime.UtcNow - startedUtc.Value).TotalSeconds;
            }
            catch { /* StartTime can be denied in some hosts; not worth failing over. */ }

            // Active PDF self-test: render a tiny document to prove the browserless
            // engine (QuestPDF + its native SkiaSharp lib) actually loads on THIS host.
            // This is the one runtime risk on locked-down shared hosting, so we verify
            // it here instead of finding out when someone clicks Download.
            var pdfRenderOk = false;
            string? pdfRenderError = null;
            try
            {
                var probe = Document.Create(d => d.Page(p =>
                {
                    p.Size(PageSizes.A4);
                    p.Content().Text("probe");
                })).GeneratePdf();
                pdfRenderOk = probe.Length > 0;
            }
            catch (Exception ex)
            {
                var chain = new List<string>();
                for (Exception? e = ex; e is not null; e = e.InnerException)
                    chain.Add($"{e.GetType().Name}: {e.Message}");
                pdfRenderError = string.Join(" || ", chain);
            }

            return Results.Ok(new
            {
                status = "ok",
                timeUtc = DateTime.UtcNow,
                environment = new
                {
                    name = env.EnvironmentName,
                    isProduction = env.IsProduction(),
                    contentRoot,
                    productionConfigPresent = File.Exists(Path.Combine(contentRoot, "appsettings.Production.json")),
                },
                // Booleans only — the actual secret values are never returned.
                configuration = new
                {
                    connectionStringConfigured = !string.IsNullOrWhiteSpace(config.GetConnectionString("CvDb")),
                    adminKeyConfigured = !string.IsNullOrWhiteSpace(config["CvAdmin:ApiKey"]),
                },
                startup = new
                {
                    databaseInitOk = StartupDiagnostics.DatabaseInitOk,
                    databaseInitError = StartupDiagnostics.DatabaseInitError,
                },
                writes = new
                {
                    lastError = StartupDiagnostics.LastWriteError,
                    lastErrorUtc = StartupDiagnostics.LastWriteErrorUtc,
                },
                database,
                pdf = new
                {
                    engine = "QuestPDF",
                    browserRequired = false,
                    renderTest = pdfRenderOk ? "ok" : "failed",
                    renderError = pdfRenderError,
                },
                runtime = new
                {
                    framework = RuntimeInformation.FrameworkDescription,
                    os = RuntimeInformation.OSDescription,
                    processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    startedUtc,
                    uptimeSeconds,
                },
            });
        });

        return routes;
    }
}

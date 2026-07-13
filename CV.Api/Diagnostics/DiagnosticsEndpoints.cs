using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CV.Api.Data;
using CV.Api.Pdf;
using Microsoft.EntityFrameworkCore;

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
///                    a fresh deploy is "not working" without RDP-ing into Plesk.
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

        routes.MapGet("/diag", async (CvDbContext db, IConfiguration config, IHostEnvironment env, CancellationToken ct) =>
        {
            var contentRoot = env.ContentRootPath;
            var chromePath = PdfSettings.ResolveChromePath(config);

            object database;
            try
            {
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
                // A bad/unreachable connection string surfaces here rather than
                // taking the whole endpoint down.
                database = new
                {
                    provider = db.Database.ProviderName,
                    canConnect = false,
                    error = ex.Message,
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
                    chromePath,
                    chromeInstalled = File.Exists(chromePath),
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

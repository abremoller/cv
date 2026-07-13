using CV.Api.Data;
using CV.Api.Diagnostics;
using CV.Api.Pdf;
using CV.Api.Security;
using CV.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Database: SQL Server when a connection string is configured (production),
//     otherwise an in-memory store so the app runs locally / in CI with no DB. ---
var connectionString = builder.Configuration.GetConnectionString("CvDb");
var useSqlServer = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDbContext<CvDbContext>(opt =>
{
    if (useSqlServer)
        opt.UseSqlServer(connectionString);
    else
        opt.UseInMemoryDatabase("CvInMemory");
});

builder.Services.AddScoped<CvStore>();

// Respect reverse-proxy headers (Plesk / nginx) so HTTPS + client IP are accurate.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// Rate-limit the admin surface to blunt brute-force attempts against the API key.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders();

// Initialise the database. Wrapped so a boot-time DB failure (bad/unreachable
// connection string, unrun migrations) doesn't take the whole site down with an
// opaque 500.30 — the app still starts and GET /api/diag reports the cause.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CvDbContext>();
        if (db.Database.IsRelational())
        {
            // Production: apply schema migrations only. Real data is loaded via the admin API.
            await db.Database.MigrateAsync();
        }
        else
        {
            // Local / CI: create the in-memory store and seed placeholder demo data.
            await db.Database.EnsureCreatedAsync();
            var store = scope.ServiceProvider.GetRequiredService<CvStore>();
            await store.SeedIfEmptyAsync(SampleData.Cv);
        }
        StartupDiagnostics.DatabaseInitOk = true;
    }
    catch (Exception ex)
    {
        StartupDiagnostics.DatabaseInitError = ex.Message;
        app.Logger.LogError(ex, "Database initialisation failed at startup; continuing so /api/diag stays reachable.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

// Serve the Blazor WebAssembly client from this same site.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// --- API ---
var api = app.MapGroup("/api");

// Health + deployment diagnostics (read-only, secret-free).
api.MapDiagnostics();

api.MapGet("/cv", async (CvStore store, CancellationToken ct) =>
{
    var cv = await store.GetAsync(ct);
    return cv is null ? Results.NotFound() : Results.Ok(cv);
});

api.MapGet("/cv/pdf", async (HttpContext ctx, IConfiguration config, CancellationToken ct) =>
{
    await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true,
        ExecutablePath = PdfSettings.ResolveChromePath(config),
        Args = ["--no-sandbox", "--disable-setuid-sandbox"]
    });

    await using var page = await browser.NewPageAsync();
    await page.SetViewportAsync(new ViewPortOptions { Width = 1200, Height = 900 });

    // Navigate to the CV page on this same server
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    await page.GoToAsync(baseUrl, new NavigationOptions
    {
        WaitUntil = [WaitUntilNavigation.Networkidle0],
        Timeout = 30_000
    });

    var pdfBytes = await page.PdfDataAsync(new PdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true,
        MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
    });

    return Results.File(pdfBytes, "application/pdf", "Abre Moller CV.pdf");
});

// Accept both POST and PUT. POST is used in production because IIS/Plesk blocks
// PUT at the server level (WebDAV / request filtering) before it reaches the app.
api.MapMethods("/cv", new[] { "POST", "PUT" }, async (CvDto cv, CvStore store, CancellationToken ct) =>
{
    await store.ReplaceAsync(cv, ct);
    return Results.NoContent();
})
.AddEndpointFilter<AdminKeyFilter>()
.RequireRateLimiting("admin");

app.MapFallbackToFile("index.html");

app.Run();

// Exposed so the integration test project can spin up the app via WebApplicationFactory.
public partial class Program;

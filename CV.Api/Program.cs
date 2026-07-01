using CV.Api.Data;
using CV.Api.Security;
using CV.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

// Initialise the database.
using (var scope = app.Services.CreateScope())
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

api.MapGet("/cv", async (CvStore store, CancellationToken ct) =>
{
    var cv = await store.GetAsync(ct);
    return cv is null ? Results.NotFound() : Results.Ok(cv);
});

api.MapPut("/cv", async (CvDto cv, CvStore store, CancellationToken ct) =>
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

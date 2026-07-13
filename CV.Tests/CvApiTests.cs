using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CV.Api.Data;
using CV.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CV.Tests;

/// <summary>
/// Spins up the real API pipeline (with the in-memory DB fallback, since no
/// connection string is configured) to verify the public read endpoint and the
/// security behaviour of the admin write endpoint end to end.
/// </summary>
public class CvApiTests
{
    private static WebApplicationFactory<Program> Factory(string? apiKey = null)
    {
        var dbName = Guid.NewGuid().ToString();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CvAdmin:ApiKey"] = apiKey,
                });
            });
            // Give each factory its own in-memory database so tests don't share state.
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CvDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<CvDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
    }

    private static CvDto NewCv(string name) => new()
    {
        Header = new HeaderDto { Name = name, Title = "Engineer" },
        Summary = $"Summary for {name}",
    };

    [Fact]
    public async Task Get_cv_returns_seeded_sample()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cv = await response.Content.ReadFromJsonAsync<CvDto>();
        cv!.Header.Name.Should().NotBeNullOrWhiteSpace();
        cv.Experience.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task Diag_reports_config_and_database_state()
    {
        using var factory = Factory(apiKey: "correct-key");
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/diag");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Reflects the configured key without leaking its value.
        json.GetProperty("configuration").GetProperty("adminKeyConfigured").GetBoolean().Should().BeTrue();
        json.GetProperty("database").GetProperty("canConnect").GetBoolean().Should().BeTrue();
        // The in-memory DB is seeded with sample data at startup.
        json.GetProperty("database").GetProperty("hasData").GetBoolean().Should().BeTrue();
        json.GetProperty("startup").GetProperty("databaseInitOk").GetBoolean().Should().BeTrue();

        // Ensure secret values are never emitted.
        var raw = await client.GetStringAsync("/api/diag");
        raw.Should().NotContain("correct-key");
    }

    [Fact]
    public async Task Put_is_fail_closed_when_no_key_configured()
    {
        using var factory = Factory(apiKey: null);
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/cv", NewCv("Nope"));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Put_with_wrong_key_is_unauthorized()
    {
        using var factory = Factory(apiKey: "correct-key");
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/cv")
        {
            Content = JsonContent.Create(NewCv("Nope")),
        };
        request.Headers.Add("X-Api-Key", "wrong-key");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_with_correct_key_updates_the_cv()
    {
        using var factory = Factory(apiKey: "correct-key");
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/cv")
        {
            Content = JsonContent.Create(NewCv("Updated Person")),
        };
        request.Headers.Add("X-Api-Key", "correct-key");

        var put = await client.SendAsync(request);
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cv = await client.GetFromJsonAsync<CvDto>("/api/cv");
        cv!.Header.Name.Should().Be("Updated Person");
    }
}

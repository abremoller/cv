using System.Security.Cryptography;
using System.Text;

namespace CV.Api.Security;

/// <summary>
/// Guards admin write endpoints with a shared secret API key.
/// Security properties:
///  - Fail-closed: if no key is configured, every request is rejected.
///  - Constant-time comparison to avoid leaking the key via timing.
///  - Key is read from configuration (env var CvAdmin__ApiKey in production); never committed.
/// Transport security (HTTPS) and rate limiting are enforced separately in Program.cs.
/// </summary>
public class AdminKeyFilter(IConfiguration config, ILogger<AdminKeyFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuredKey = config["CvAdmin:ApiKey"];

        // Fail closed: no key configured means the admin surface is disabled.
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            logger.LogWarning("Admin endpoint called but CvAdmin:ApiKey is not configured; rejecting.");
            return Results.Problem("Admin API is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var http = context.HttpContext;
        var provided = http.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(provided) || !FixedTimeEquals(provided, configuredKey))
        {
            logger.LogWarning("Rejected admin request from {Ip} (bad or missing API key).",
                http.Connection.RemoteIpAddress);
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

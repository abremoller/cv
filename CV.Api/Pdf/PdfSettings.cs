namespace CV.Api.Pdf;

/// <summary>
/// Central resolution of the headless Chrome executable used to render the CV to
/// PDF. Overridable via the "Pdf:ChromePath" configuration key so the production
/// (Plesk / Windows) host can point at wherever Chrome actually lives — and so
/// /api/diag can report whether that path exists before the PDF endpoint is hit.
/// </summary>
public static class PdfSettings
{
    public const string DefaultChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    public static string ResolveChromePath(IConfiguration config)
    {
        var configured = config["Pdf:ChromePath"];
        return string.IsNullOrWhiteSpace(configured) ? DefaultChromePath : configured;
    }
}

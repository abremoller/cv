namespace CV.Api.Pdf;

/// <summary>
/// Resolves a Chromium-based browser executable for headless PDF rendering.
/// Prefers an explicit "Pdf:ChromePath" config value; otherwise probes the common
/// install locations for Chrome and Microsoft Edge (Edge ships with Windows Server,
/// so this usually works with no changes on the host). /api/diag reports the result.
/// </summary>
public static class PdfSettings
{
    public const string DefaultChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    // Checked in order; first existing path wins. Edge is Chromium-based and drives
    // fine via PuppeteerSharp, so it's a reliable fallback when Chrome isn't installed.
    private static readonly string[] CandidatePaths =
    [
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ];

    /// <summary>
    /// Returns a usable browser executable path, or null if none is found.
    /// A configured path is honoured only if it actually exists, so a stale
    /// override can't shadow an available browser.
    /// </summary>
    public static string? ResolveBrowserPath(IConfiguration config)
    {
        var configured = config["Pdf:ChromePath"];
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        return CandidatePaths.FirstOrDefault(File.Exists);
    }
}

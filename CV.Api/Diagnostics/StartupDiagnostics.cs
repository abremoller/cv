namespace CV.Api.Diagnostics;

/// <summary>
/// Captures one-time startup outcomes (currently database initialisation) so the
/// /api/diag endpoint can report them even when the underlying resource is
/// unavailable. If the DB is unreachable at boot the app still starts (see
/// Program.cs) and diag surfaces the recorded error, instead of the whole site
/// failing with an opaque 500.30 and no clue as to why.
/// </summary>
public static class StartupDiagnostics
{
    public static bool DatabaseInitOk { get; set; }
    public static string? DatabaseInitError { get; set; }

    /// <summary>Message of the most recent failed CV write, surfaced via /api/diag.</summary>
    public static string? LastWriteError { get; set; }
    public static DateTime? LastWriteErrorUtc { get; set; }
}

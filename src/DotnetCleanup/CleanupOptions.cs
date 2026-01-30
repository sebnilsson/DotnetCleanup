namespace DotnetCleanup;

public enum VerbosityLevel
{
    Minimal,
    Normal,
    Detailed,
    Debug
}

internal sealed record CleanupOptions(
    string RootPath,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string> ExcludePatterns,
    string TempPath,
    bool ConfirmCleanup,
    bool NoDelete,
    bool NoMove,
    VerbosityLevel Verbosity,
    DateTimeOffset StartedAt);

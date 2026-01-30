using Spectre.Console.Cli;

namespace DotnetCleanup;

public sealed class CleanupSettings : CommandSettings
{
    [CommandArgument(0, "[PATH]")]
    public string? Path { get; init; }

    [CommandOption("-p|--paths <PATHS>")]
    public string[] Paths { get; init; } = Array.Empty<string>();

    [CommandOption("-x|--exclude <PATTERNS>")]
    public string[] Exclude { get; init; } = Array.Empty<string>();

    [CommandOption("-y|--confirm-cleanup")]
    public bool ConfirmCleanup { get; init; }

    [CommandOption("--no-delete")]
    public bool NoDelete { get; init; }

    [CommandOption("--no-move")]
    public bool NoMove { get; init; }

    [CommandOption("-t|--temp-path <PATH>")]
    public string? TempPath { get; init; }

    [CommandOption("-v|--verbosity <LEVEL>")]
    public VerbosityLevel Verbosity { get; init; } = VerbosityLevel.Normal;
}

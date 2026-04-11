using System.ComponentModel;
using DotnetCleanup.IO;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed partial class CleanupSettings : CommandSettings
{
    public const string DefaultIncludePaths = "**/bin, **/obj, **/node_modules";
    private static readonly string[] s_defaultIncludePathList = DefaultIncludePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public CleanupSettings(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        Path = fileSystem.GetCurrentDirectory();
        Include = [.. s_defaultIncludePathList];
        TempPath = fileSystem.GetTempPath();
    }

    [CommandArgument(0, "[PATH]")]
    [Description("The starting path for the cleanup. Defaults to current directory.")]
    public string Path { get; init; }

    [CommandOption("-p|--include <PATTERNS>")]
    [Description($"Glob paths to include in cleanup. Default paths: {DefaultIncludePaths}.")]
    public string[] Include { get; init; }

    [CommandOption("-x|--exclude <PATTERNS>")]
    [Description("Glob paths to exclude from cleanup.")]
    public string[] Exclude { get; init; } = [];

    [CommandOption("-y|--yes")]
    [Description("Run cleanup skipping confirm prompt.")]
    public bool SkipConfirm { get; init; }

    [CommandOption("--noop|--whatif|--what-if")]
    [Description("No-op mode: list matching paths without moving or deleting anything. Equivalent to --no-move and --no-delete.")]
    public bool Noop { get; init; }

    [CommandOption("--no-delete")]
    [Description("Skip deleting matched paths after moving them to temporary folder. Ignored when --noop is used.")]
    public bool SkipDelete { get; init; }

    [CommandOption("--no-move")]
    [Description("Skip moving matched paths to temporary folder before deletion. Ignored when --noop is used.")]
    public bool SkipMove { get; init; }

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    [CommandOption("--temp-path <PATH>")]
    [Description("Temporary path to move cleanup files before deletion.")]
    public string TempPath { get; init; }

    [CommandOption("-v|--verbosity <LEVEL>")]
    [Description("Sets the verbosity level. Allowed values are minimal (m), normal (n) and detailed (d).")]
    public VerbosityLevelOptions VerbositySetting
    {
        set
        {
            Verbosity = value switch
            {
                VerbosityLevelOptions.M or VerbosityLevelOptions.Minimal => VerbosityLevel.Minimal,
                VerbosityLevelOptions.N or VerbosityLevelOptions.Normal => VerbosityLevel.Normal,
                VerbosityLevelOptions.D or VerbosityLevelOptions.Detailed => VerbosityLevel.Detailed,
                _ => VerbosityLevel.Normal
            };
        }
    }

    public VerbosityLevel Verbosity { get; private set; } = VerbosityLevel.Normal;

    public enum VerbosityLevelOptions
    {
        M,
        Minimal,
        N,
        Normal,
        D,
        Detailed,
    }

    public bool IsVerbosityNormal() => IsVerbosity(VerbosityLevel.Normal);

    public bool IsVerbosityDetailed() => IsVerbosity(VerbosityLevel.Detailed);

    public bool IsVerbosity(VerbosityLevel verbosityLevel) => Verbosity >= verbosityLevel;

    public bool ShouldSkipMove() => Noop || SkipMove;

    public bool ShouldSkipDelete() => Noop || SkipDelete;
}

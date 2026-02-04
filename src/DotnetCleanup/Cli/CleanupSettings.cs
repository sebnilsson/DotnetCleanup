using System.ComponentModel;
using DotnetCleanup.IO;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed partial class CleanupSettings(IFileSystem fileSystem) : CommandSettings
{
    public const string DefaultIncludePaths = "bin, obj, node_modules";

    public readonly string[] DefaultIncludePathList = DefaultIncludePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [CommandArgument(0, "[PATH]")]
    [Description("The starting path for the cleanup.")]
    public string Path { get; init; } = fileSystem.GetCurrentDirectory();

    [CommandOption("-y|--yes|--confirm")]
    [Description("Run cleanup without prompt.")]
    public bool ConfirmCleanup { get; init; }

    [CommandOption("-p|--include <PATTERNS>")]
    [Description($"Glob paths to include in cleanup. Default paths: {DefaultIncludePaths}.")]
    public string[] Include { get; init; } = [];

    [CommandOption("-x|--exclude <PATTERNS>")]
    [Description("Glob paths to exclude from cleanup.")]
    public string[] Exclude { get; init; } = [];

    [CommandOption("--noop|--whatif|--what-if")]
    [Description("Skip deleting files.")]
    public bool SkipDelete { get; init; }

    [CommandOption("--no-move")]
    [Description("Skip moving files to temporary folder before deletion.")]
    public bool SkipMove { get; init; }

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    [CommandOption("--temp-path <PATH>")]
    [Description("Temporary path to move cleanup files before deletion.")]
    public string TempPath { get; init; } = fileSystem.GetTempPath();

    [CommandOption("-v|--verbosity <LEVEL>")]
    [Description("Sets the verbosity level. Allowed values are minimal (m), normal (n) and detailed (d).")]
    public VerbosityLevelSettings VerbositySetting
    {
        set
        {
            Verbosity = value switch
            {
                VerbosityLevelSettings.M or VerbosityLevelSettings.Minimal => VerbosityLevel.Minimal,
                VerbosityLevelSettings.N or VerbosityLevelSettings.Normal => VerbosityLevel.Normal,
                VerbosityLevelSettings.D or VerbosityLevelSettings.Detailed => VerbosityLevel.Detailed,
                _ => VerbosityLevel.Normal
            };
        }
    }

    public VerbosityLevel Verbosity { get; private set; } = VerbosityLevel.Normal;

    public enum VerbosityLevelSettings
    {
        M,
        Minimal,
        N,
        Normal,
        D,
        Detailed,
    }

    public bool IsVerbosityNormal() => Verbosity >= VerbosityLevel.Normal;

    public bool IsVerbosityDetailed() => Verbosity >= VerbosityLevel.Detailed;
}

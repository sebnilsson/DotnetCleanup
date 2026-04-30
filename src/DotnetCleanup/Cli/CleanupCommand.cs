using DotnetCleanup.IO;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed class CleanupCommand(CleanupService service, IAnsiConsole console, IFileSystem fileSystem)
    : AsyncCommand<CleanupSettings>
{
    private static readonly Lock s_consoleWriteLock = new();
    private readonly IAnsiConsole _console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly CleanupService _service = service ?? throw new ArgumentNullException(nameof(service));

    protected override ValidationResult Validate(CommandContext context, CleanupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        var validationResult = base.Validate(context, settings);
        if (!validationResult.Successful)
        {
            return validationResult;
        }

        if (settings.Include.Length == 0)
        {
            return ValidationResult.Error("At least one include pattern must be specified.");
        }
        if (!_fileSystem.DirectoryExists(settings.Path))
        {
            return ValidationResult.Error($"The given path does not exist: {settings.Path}");
        }
        if (!settings.ShouldSkipMove() && !_fileSystem.DirectoryExists(settings.TempPath))
        {
            return ValidationResult.Error($"The given temporary path does not exist: {settings.TempPath}");
        }

        return ValidationResult.Success();
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, CleanupSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        _service.OnListPathsStepStart += () =>
            WriteStepStart(settings, "Finding paths...", ":magnifying_glass_tilted_right:");

        _service.OnMovePathsStepStart += () =>
            WriteStepStart(settings, "Moving paths...", ":open_file_folder:", settings.ShouldSkipMove(), "Skipping moving paths");

        _service.OnDeletePathsStepStart += () =>
            WriteStepStart(settings, "Deleting paths...", ":cross_mark:", settings.ShouldSkipDelete(), "Skipping deleting paths");

        _service.OnListPathsStepDone += step => WriteStepCompleted(settings, step, "Find");
        _service.OnMovePathsStepDone += step => WriteStepCompleted(settings, step, "Move", settings.ShouldSkipMove());
        _service.OnDeletePathsStepDone += step => WriteStepCompleted(settings, step, "Delete", settings.ShouldSkipDelete());

        _service.OnListPath += path => WriteOnPath(
            settings,
            path,
            "gray",
            "Error listing path",
            settings.SkipConfirm ? VerbosityLevel.Detailed : VerbosityLevel.Normal);
        _service.OnMovePath += path => WriteOnPath(settings, path, "cyan", "Error moving path");
        _service.OnDeletePath += path => WriteOnPath(settings, path, "Purple_1", "Error deleting path");

        var isConfirmed = false;
        var result = _service.Cleanup(onConfirm: () =>
        {
            isConfirmed = ConfirmCleanup(settings);
            return isConfirmed;
        }, settings, cancellationToken);

        if (isConfirmed)
        {
            WriteCleanupCompletion(result, settings);
        }

        return 0;
    }

    private bool ConfirmCleanup(CleanupSettings settings)
    {
        if (settings.SkipConfirm)
        {
            return true;
        }

        var isConfirmed = _console.Confirm("Proceed with the cleanup?", defaultValue: false);
        if (!isConfirmed)
        {
            _console.MarkupLine("[yellow]Cleanup canceled by user[/]");
        }

        return isConfirmed;
    }

    private void WriteStepStart(CleanupSettings settings, string titleText, string emoji, bool? skipStep = null, string? skipText = null)
    {
        if (skipStep != true && settings.IsVerbosityDetailed())
        {
            _console.Write(new Rule($"{emoji} {titleText}")
            {
                Border = BoxBorder.Ascii
            });
        }
        else
        {
            WriteVerbosityNormal(settings, $"[cyan]{skipText}[/]");
        }
    }

    private void WriteOnPath(CleanupSettings settings, PathInfo path, string color, string errorText, VerbosityLevel verbosityLevel = VerbosityLevel.Detailed)
    {
        lock (s_consoleWriteLock)
        {
            var escapedPath = Markup.Escape(path.Value);

            if (path.Exception != null && settings.IsVerbosityNormal())
            {
                _console.Markup($"[red]{errorText}: {escapedPath}[/]");
                if (settings.IsVerbosity(verbosityLevel))
                {
                    _console.Write(" -- ");
                    _console.WriteException(path.Exception, ExceptionFormats.NoStackTrace);
                }
                else
                {
                    _console.WriteLine();
                }
            }
            else if (settings.IsVerbosity(verbosityLevel))
            {
                _console.MarkupLine($"[{color}]{escapedPath}[/]");
            }
        }
    }

    private void WriteStepCompleted(CleanupSettings settings, CleanupStep step, string stepName, bool skipStep = false)
    {
        if (!skipStep && settings.IsVerbosityDetailed())
        {
            if (step.Successes.Count == 0 && step.Failed.Count == 0)
            {
                _console.MarkupLine("[yellow]No matching paths found[/]");
                return;
            }

            _console.MarkupLine($"[blue]{stepName} step completed.[/]");
            _console.MarkupLine($"  {FormatStepSummaryText(step, "blue")}");
        }
    }

    private void WriteCleanupCompletion(CleanupResult result, CleanupSettings settings)
    {
        _console.MarkupLine("[green]:check_mark:  Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal())
        {
            _console.MarkupLine($"  Found: {FormatStepSummaryText(result.ListStep, "green")}");

            if (!settings.ShouldSkipMove())
            {
                _console.MarkupLine($"  Moved: {FormatStepSummaryText(result.MoveStep, "green")}");
            }
        }

        if (!settings.ShouldSkipDelete())
        {
            _console.MarkupLine($"  Deleted: {FormatStepSummaryText(result.DeleteStep, "green")}");
        }
    }

    private void WriteVerbosityDetailed(CleanupSettings settings, string message)
    {
        if (settings.IsVerbosityDetailed())
        {
            _console.MarkupLine(message);
        }
    }

    private void WriteVerbosityNormal(CleanupSettings settings, string message)
    {
        if (settings.IsVerbosityNormal())
        {
            _console.MarkupLine(message);
        }
    }

    private static string FormatStepSummaryText(CleanupStep? step, string successColor = "blue")
    {
        if (step == null)
        {
            return string.Empty;
        }

        var errorText = step.Failed.Count > 0 ? $" [red]({step.Failed.Count} failed)[/]" : string.Empty;

        return $"[{successColor}]{step.Successes.Count} paths[/]{errorText}";
    }
}

using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed class CleanupCommand(CleanupService service, IAnsiConsole console)
    : AsyncCommand<CleanupSettings>
{
    private static readonly Lock s_consoleWriteLock = new();
    private readonly IAnsiConsole _console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly CleanupService _service = service ?? throw new ArgumentNullException(nameof(service));

    public override async Task<int> ExecuteAsync(CommandContext context, CleanupSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        AttachEventHandlers(settings);

        var result = _service.Cleanup(() => ConfirmCleanup(settings), settings, cancellationToken);

        WriteCompletion(result, settings);

        return 0;
    }

    private void AttachEventHandlers(CleanupSettings settings)
    {
        _service.OnListPathsStepStart += () =>
        {
            WriteVerbosityDetailed(settings, ":magnifying_glass_tilted_right: Listing files...");
        };

        _service.OnMovePathsStepStart += () =>
        {
            if (settings.ShouldSkipMove())
            {
                WriteVerbosityNormal(settings, "[cyan]Skipping moving files[/]");
            }
            else
            {
                WriteVerbosityDetailed(settings, ":open_file_folder: Moving files...");
            }
        };

        _service.OnDeletePathsStepStart += () =>
        {
            if (settings.ShouldSkipDelete())
            {
                WriteVerbosityNormal(settings, "[cyan]Skipping deleting files[/]");
            }
            else
            {
                WriteVerbosityDetailed(settings, ":cross_mark: Deleting files...");
            }
        };

        _service.OnListPathsStepDone += step =>
        {
            if (step.Successes.Count == 0)
            {
                _console.MarkupLine("[yellow]No files found[/]");
                return;
            }

            WriteVerbosityNormal(settings, $"[blue]{GetFilesFoundText(step.Successes.Count)}[/]");
        };

        _service.OnMovePathsStepDone += step => WriteStepCompleted(settings, settings.ShouldSkipMove(), step, "Move");
        _service.OnDeletePathsStepDone += step => WriteStepCompleted(settings, settings.ShouldSkipDelete(), step, "Delete");

        _service.OnListPath += path => WriteOnPath(settings, path, "gray", "Error listing");
        _service.OnMovePath += path => WriteOnPath(settings, path, "cyan", "Error moving");
        _service.OnDeletePath += path => WriteOnPath(settings, path, "Purple_1", "Error deleting", useMovePathForFailure: true);
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

    private void WriteCompletion(CleanupResult result, CleanupSettings settings)
    {
        _console.MarkupLine("[green]:check_mark:  Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal())
        {
            WriteStepSummary(result.LastExecutedStep, "green");
        }
    }

    private void WriteOnPath(CleanupSettings settings, PathInfo path, string color, string errorText, bool useMovePathForFailure = false)
    {
        lock (s_consoleWriteLock)
        {
            if (path.Exception == null)
            {
                if (settings.IsVerbosityDetailed())
                {
                    _console.MarkupLine($"[{color}]{path.Value}[/]");
                }

                return;
            }

            var displayPath = useMovePathForFailure && !string.IsNullOrWhiteSpace(path.MovePath)
                ? path.MovePath
                : path.Value;

            _console.MarkupLine($"[red]{errorText}: {displayPath}[/]");
            _console.WriteException(path.Exception, ExceptionFormats.NoStackTrace);
        }
    }

    private void WriteStepCompleted(CleanupSettings settings, bool skipStep, CleanupStep step, string stepName, string color = "blue")
    {
        if (!skipStep && settings.IsVerbosityDetailed())
        {
            if (step.Successes.Count == 0)
            {
                _console.MarkupLine("[yellow]No files found[/]");
                return;
            }

            _console.MarkupLine($"[{color}]{stepName} step completed.[/]");
            WriteStepSummary(step);
        }
    }

    private void WriteStepSummary(CleanupStep step, string successColor = "blue")
    {
        _console.MarkupLine(FormatStepSummaryText(step, successColor));
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

    private static string GetFilesFoundText(int count)
    {
        var noun = count == 1 ? "file" : "files";
        return $"{count} {noun} found";
    }

    private static string FormatStepSummaryText(CleanupStep step, string successColor = "blue")
    {
        var errorText = step.Failed.Count > 0 ? $" [red]{step.Failed.Count} failed.[/]" : string.Empty;

        return $"  [{successColor}]{step.Successes.Count} succeeded.[/]{errorText}";
    }
}

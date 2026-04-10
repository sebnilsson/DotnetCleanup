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

        var isConfirmed = false;
        var result = _service.Cleanup(() =>
        {
            isConfirmed = ConfirmCleanup(settings);
            return isConfirmed;
        }, settings, cancellationToken);

        if (isConfirmed)
        {
            WriteCompletion(result, settings);
        }

        return 0;
    }

    private void AttachEventHandlers(CleanupSettings settings)
    {
        _service.OnListPathsStepStart += () =>
        {
            WriteTitle(settings, ":magnifying_glass_tilted_right: Finding paths...");
        };

        _service.OnMovePathsStepStart += () =>
        {
            if (!settings.ShouldSkipMove())
            {
                WriteTitle(settings, ":open_file_folder: Moving paths...");
            }
            else
            {
                WriteVerbosityNormal(settings, "[cyan]Skipping moving paths[/]");
            }
        };

        _service.OnDeletePathsStepStart += () =>
        {
            if (!settings.ShouldSkipDelete())
            {
                WriteTitle(settings, ":cross_mark: Deleting paths...");
            }
            else
            {
                WriteVerbosityNormal(settings, "[cyan]Skipping deleting paths[/]");
            }
        };

        _service.OnListPathsStepDone += step => WriteListStepCompleted(settings, step);
        _service.OnMovePathsStepDone += step => WriteStepCompleted(settings, settings.ShouldSkipMove(), step, "Move", "blue");
        _service.OnDeletePathsStepDone += step => WriteStepCompleted(settings, settings.ShouldSkipDelete(), step, "Delete", "blue");

        _service.OnListPath += path => WriteOnPath(settings, path, "gray", "Error listing path", verbosityLevel: VerbosityLevel.Normal);
        _service.OnMovePath += path => WriteOnPath(settings, path, "cyan", "Error moving path");
        _service.OnDeletePath += path => WriteOnPath(settings, path, "Purple_1", "Error deleting path", useMovePathForFailure: true);
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

    private void WriteTitle(CleanupSettings settings, string title)
    {
        if (settings.IsVerbosityNormal())
        {
            _console.Write(new Rule(title)
            {
                Border = BoxBorder.Ascii
            });
        }
    }

    private void WriteCompletion(CleanupResult result, CleanupSettings settings)
    {
        _console.MarkupLine("[green]:check_mark:  Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal())
        {
            WriteStepSummary(result.LastExecutedStep, "green");
        }
    }

    private void WriteOnPath(CleanupSettings settings, PathInfo path, string color, string errorText, VerbosityLevel verbosityLevel = VerbosityLevel.Detailed, bool useMovePathForFailure = false)
    {
        lock (s_consoleWriteLock)
        {
            if (path.Exception != null && settings.IsVerbosityNormal())
            {
                var displayPath = useMovePathForFailure && !string.IsNullOrWhiteSpace(path.MovePath)
                    ? path.MovePath
                    : path.Value;

                _console.Markup($"[red]{errorText}: {displayPath}[/] -- ");
                _console.WriteException(path.Exception, ExceptionFormats.NoStackTrace);
            }
            else if (settings.IsVerbosity(verbosityLevel))
            {
                _console.MarkupLine($"[{color}]{path.Value}[/]");
            }
        }
    }

    private void WriteStepCompleted(CleanupSettings settings, bool skipStep, CleanupStep step, string stepName, string color = "blue")
    {
        if (!skipStep && settings.IsVerbosityDetailed())
        {
            if (step.Successes.Count == 0 && step.Failed.Count == 0)
            {
                _console.MarkupLine("[yellow]No matching paths found[/]");
                return;
            }

            _console.MarkupLine($"[{color}]{stepName} step completed.[/]");
            WriteStepSummary(step);
        }
    }

    private void WriteListStepCompleted(CleanupSettings settings, CleanupStep step)
    {
        if (step.Successes.Count == 0)
        {
            if (step.Failed.Count == 0)
            {
                _console.MarkupLine("[yellow]No matching paths found[/]");
            }
            else
            {
                WriteVerbosityNormal(settings, "[yellow]Listing completed with failures[/]");
                WriteStepSummary(step);
            }

            return;
        }

        WriteVerbosityNormal(settings, $"[blue]{GetPathsFoundText(step.Successes.Count)}[/]");

        if (step.Failed.Count > 0 && settings.IsVerbosityNormal())
        {
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

    private static string GetPathsFoundText(int count)
    {
        var noun = count == 1 ? "path" : "paths";
        return $"{count} {noun} found";
    }

    private static string FormatStepSummaryText(CleanupStep step, string successColor = "blue")
    {
        var errorText = step.Failed.Count > 0 ? $" [red]{step.Failed.Count} failed.[/]" : string.Empty;

        return $"  [{successColor}]{step.Successes.Count} succeeded.[/]{errorText}";
    }
}

using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed class CleanupCommand(CleanupService service, IAnsiConsole console)
    : AsyncCommand<CleanupSettings>
{
    private static readonly Lock s_consoleWriteLock = new();

    public override async Task<int> ExecuteAsync(CommandContext context, CleanupSettings settings, CancellationToken cancellationToken)
    {
        service.OnListPathsStepStart += () =>
        {
            WriteVerbosityDetailed(":magnifying_glass_tilted_right: Listing files...");
        };

        service.OnMovePathsStepStart += () =>
        {
            if (settings.SkipMove)
            {
                WriteVerbosityNormal("[cyan]Skipping moving files[/]");
            }
            else
            {
                WriteVerbosityDetailed(":open_file_folder: Moving files...");
            }
        };

        service.OnDeletePathsStepStart += () =>
        {
            if (settings.SkipDelete)
            {
                WriteVerbosityNormal("[cyan]Skipping deleting files[/]");
            }
            else
            {
                WriteVerbosityDetailed(":cross_mark: Deleting files...");
            }
        };

        service.OnListPathsStepDone += (step) =>
        {
            if (step.Successes.Count == 0)
            {
                console.MarkupLine("[yellow]No files found[/]");
                return;
            }

            WriteVerbosityNormal($"[blue]{step.Successes.Count} files found[/]");
        };

        service.OnMovePathsStepDone += (step) => WriteStepCompleted(settings.SkipMove, step, "Move");
        service.OnDeletePathsStepDone += (step) => WriteStepCompleted(settings.SkipDelete, step, "Delete");

        service.OnListPath += (path) => WriteOnPath(path, "gray", "Error listing");
        service.OnMovePath += (path) => WriteOnPath(path, "cyan", "Error moving");
        service.OnDeletePath += (path) => WriteOnPath(path, "Purple_1", "Error deleting");

        var result = await console.Status().StartAsync(":broom: Cleanup running...",
            async (_) => service.Cleanup(() =>
            {
                if (!settings.SkipConfirm)
                {
                    var isConfirmed = console.Confirm("Proceed with the cleanup?", defaultValue: false);

                    if (!isConfirmed)
                    {
                        console.MarkupLine("[yellow]Cleanup canceled by user[/]");
                    }

                    return isConfirmed;
                }
                else
                {
                    return true;
                }
            },
            settings,
            cancellationToken));

        console.MarkupLine($"[green]:check_mark:  Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal())
        {
            WriteStepSummary(result.DeleteStep, "green");
        }

        return 0;

        void WriteOnPath(PathInfo path, string color, string errorText)
        {
            lock (s_consoleWriteLock)
            {
                if (path.Exception == null)
                {
                    if (settings.IsVerbosityDetailed())
                    {
                        console.MarkupLine($"[{color}]{path.Value}[/]");
                    }
                }
                else
                {
                    console.MarkupLine($"[red]{errorText}: {path.Value}[/]");
                    console.WriteException(path.Exception, ExceptionFormats.NoStackTrace);
                }
            }
        }

        void WriteStepCompleted(bool skipStep, CleanupStep step, string stepName, string color = "blue")
        {
            if (!skipStep && settings.IsVerbosityDetailed())
            {
                if (step.Successes.Count == 0)
                {
                    console.MarkupLine("[yellow]No files found[/]");
                    return;
                }

                console.MarkupLine($"[{color}]{stepName} step completed.[/]");
                WriteStepSummary(step);
            }
        }

        void WriteStepSummary(CleanupStep step, string successColor = "blue")
        {
            console.MarkupLine(GetStepSuccessErrorText(step, successColor));
        }

        void WriteVerbosityDetailed(string message)
        {
            if (settings.IsVerbosityDetailed())
            {
                console.MarkupLine(message);
            }
        }

        void WriteVerbosityNormal(string message)
        {
            if (settings.IsVerbosityNormal())
            {
                console.MarkupLine(message);
            }
        }
    }

    private static string GetStepSuccessErrorText(CleanupStep step, string successColor = "blue")
    {
        var errorText = step.Failed.Count > 0 ? $" [red]{step.Failed.Count} failed.[/]" : string.Empty;

        return $"  [{successColor}]{step.Successes.Count} succeeded.[/]{errorText}";
    }
}

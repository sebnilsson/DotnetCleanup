using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed class CleanupCommand(CleanupService service, IAnsiConsole console)
    : AsyncCommand<CleanupSettings>
{
    private static readonly Lock s_consoleWriteLock = new();

    public override async Task<int> ExecuteAsync(CommandContext context, CleanupSettings settings, CancellationToken cancellationToken)
    {
        WriteVerbosityNormal(":broom: Cleanup started");

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

        service.OnMovePathsStepDone += (step) =>
        {
            if (!settings.SkipMove)
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"[blue]Move step completed.[/]");
                    console.MarkupLine(GetStepSuccessErrorText(step));
                }
            }
        };

        service.OnDeletePathsStepDone += (step) =>
        {
            if (!settings.SkipDelete)
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"[blue]Delete step completed.[/]");
                    console.MarkupLine(GetStepSuccessErrorText(step));
                }
            }
        };

        service.OnListPath += (path) =>
        {
            WriteOnPath(path, "gray", "Error listing");
        };

        service.OnMovePath += (path) =>
        {
            WriteOnPath(path, "cyan", "Error moving");
        };
        service.OnDeletePath += (path) =>
        {
            WriteOnPath(path, "Purple_1", "Error deleting");
        };

        var result = service.Cleanup(() =>
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
        cancellationToken);

        console.MarkupLine($"[green]:check_mark:  Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal() && result.DeleteStep != null)
        {
            console.MarkupLine(GetStepSuccessErrorText(result.DeleteStep, successColor: "green"));
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
        var errorText = step.Errors.Count > 0 ? $" [red]{step.Errors.Count} failed.[/]" : string.Empty;

        return $"  [{successColor}]{step.Successes.Count} succeeded.[/]{errorText}";
    }
}

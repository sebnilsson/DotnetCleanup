using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public sealed class CleanupCommand(CleanupService service, IAnsiConsole console)
    : AsyncCommand<CleanupSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CleanupSettings settings, CancellationToken cancellationToken)
    {
        if (settings.IsVerbosityDetailed())
        {
            console.MarkupLine("Cleanup started");
        }

        service.OnGetPathsStepStart += () =>
        {
            if (settings.IsVerbosityDetailed())
            {
                console.MarkupLine($"[blue]Get step started.[/]");
            }
        };

        service.OnGetPathsStepDone += (step) =>
        {
            if (step.Successes.Count == 0)
            {
                console.MarkupLine("[yellow]No files found[/]");
            }
            else if (settings.IsVerbosityNormal())
            {
                console.MarkupLine($"[blue]{step.Successes.Count} files found[/]");
            }
        };

        service.OnMovePathsStepStart += () =>
        {
            if (settings.SkipMove)
            {
                if (settings.IsVerbosityNormal())
                {
                    console.MarkupLine($"[cyan]Skipping moving files[/]");
                }
            }
            else
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"Moving files...");
                }
            }
        };

        service.OnMovePathsStepDone += (step) =>
        {
            if (!settings.SkipMove)
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"[blue]Move step completed.[/]");
                    console.MarkupLine($"  [blue]{step.Successes.Count} succeeded[/], [red]{step.Errors.Count} failed[/]");
                }
            }
        };

        service.OnDeletePathsStepStart += () =>
        {
            if (settings.SkipDelete)
            {
                if (settings.IsVerbosityNormal())
                {
                    console.MarkupLine($"[cyan]Skipping deleting files[/]");
                }
            }
            else
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"Deleting files...");
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
                    console.MarkupLine($"  [blue]{step.Successes.Count} succeeded[/], [red]{step.Errors.Count} failed[/]");
                }
            }
        };

        service.OnGetPath += (path) =>
        {
            if (path.Exception == null)
            {
                if (settings.IsVerbosityNormal())
                {
                    console.MarkupLine($"[gray]{path.Normalized}[/]");
                }
            }
            else
            {
                console.MarkupLine($"[red]Error processing: {path.Value}[/]");
                console.WriteException(path.Exception, ExceptionFormats.ShortenEverything);
            }
        };

        service.OnMovePath += (path) =>
        {
            if (path.Exception == null)
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"[cyan]{path.Normalized}[/]");
                }
            }
            else
            {
                console.MarkupLine($"[red]Error moving: {path.Value}[/]");
                console.WriteException(path.Exception, ExceptionFormats.ShortenEverything);
            }
        };

        service.OnDeletePath += (path) =>
        {
            if (path.Exception == null)
            {
                if (settings.IsVerbosityDetailed())
                {
                    console.MarkupLine($"[Purple_1]{path.Normalized}[/]");
                }
            }
            else
            {
                console.MarkupLine($"[red]Error deleting: {path.Value}[/]");
                console.WriteException(path.Exception, ExceptionFormats.ShortenEverything);
            }
        };

        var result = service.Cleanup(() =>
        {
            var runCleanup = !settings.ConfirmCleanup || console.Confirm("Proceed with the cleanup?", defaultValue: false);

            if (settings.ConfirmCleanup)
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

        console.MarkupLine($"[green]Cleanup process completed.[/]");

        if (settings.IsVerbosityNormal() && result.DeleteStep != null)
        {
            console.MarkupLine($"  [green]{result.DeleteStep.Successes.Count} succeeded[/], [red]{result.DeleteStep.Errors.Count} failed[/]");
        }

        return 0;
    }
}

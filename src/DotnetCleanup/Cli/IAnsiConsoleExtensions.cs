using Spectre.Console;

namespace DotnetCleanup.Cli;

public static class IAnsiConsoleExtensions
{
    public static void WriteVerbosityDetailed(this IAnsiConsole console, string message, CleanupSettings settings)
    {
        if (settings.IsVerbosityDetailed())
        {
            console.MarkupLine(message);
        }
    }

    public static void WriteVerbosityNormal(this IAnsiConsole console, string message, CleanupSettings settings)
    {
        if (settings.IsVerbosityNormal())
        {
            console.MarkupLine(message);
        }
    }
}

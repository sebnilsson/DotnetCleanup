using System;

using McMaster.Extensions.CommandLineUtils;

namespace DotnetCleanup
{
    public static class ConsoleExtensions
    {
        public static void SetColors(
            this IConsole console,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            if (foregroundColor != null)
                console.ForegroundColor = foregroundColor.Value;
            if (backgroundColor != null)
                console.BackgroundColor = backgroundColor.Value;
        }

        public static IDisposable ColorContext(
            this IConsole console,
            ConsoleColors.Profile colorProfile)
        {
            return new ConsoleColorContext(console, colorProfile);
        }
    }
}
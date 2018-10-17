using System;

using McMaster.Extensions.CommandLineUtils;

namespace DotnetCleanup
{
    public class ConsoleColorContext : IDisposable
    {
        private readonly IConsole _console;

        public ConsoleColorContext(
            IConsole console,
            ConsoleColors.Profile colorProfile)
        {
            _console = console;

            _console.SetColors(
                colorProfile.Foreground,
                colorProfile.Background);
        }

        public void Dispose()
        {
            _console.ResetColor();
        }
    }
}
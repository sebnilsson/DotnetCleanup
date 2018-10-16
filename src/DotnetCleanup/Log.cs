using System;

using KeyLocks;
using McMaster.Extensions.CommandLineUtils;

namespace DotnetCleanup
{
    internal class Log : ILog
    {
        private readonly KeyLock<ILog> _logLock;
        private readonly CommandContext _context;
        private readonly IConsole _console;

        public Log(
            CommandContext context,
            IConsole console,
            KeyLock<ILog> logLock)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            _console = console
                ?? throw new ArgumentNullException(nameof(console));
            _logLock = logLock
                ?? throw new ArgumentNullException(nameof(logLock));
        }

        public void Debug(string value = null)
        {
            Write(
                LogLevel.Debug,
                $"DEBUG: {value}",
                ConsoleColors.Debug.Foreground,
                ConsoleColors.Debug.Background);
        }

        public void Detailed(
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            Write(
                LogLevel.Detailed,
                value,
                foregroundColor,
                backgroundColor);
        }

        public void Minimal(
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            Write(
                LogLevel.Minimal,
                value,
                foregroundColor,
                backgroundColor);
        }

        public void Normal(
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            Write(
                LogLevel.Normal,
                value,
                foregroundColor,
                backgroundColor);
        }

        public void Write(
            LogLevel level,
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            if (level > _context.Verbosity)
                return;

            _logLock.RunWithLock(this,
                () =>
                {
                    if (foregroundColor != null)
                        _console.ForegroundColor = foregroundColor.Value;
                    if (backgroundColor != null)
                        _console.BackgroundColor = backgroundColor.Value;

                    _console.WriteLine(value);

                    _console.ResetColor();
                });
        }
    }
}
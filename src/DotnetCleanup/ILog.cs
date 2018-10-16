using System;

namespace DotnetCleanup
{
    public interface ILog
    {
        void Debug(string value = null);

        void Detailed(
               string value = null,
               ConsoleColor? foregroundColor = null,
               ConsoleColor? backgroundColor = null);

        void Normal(
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null);

        void Minimal(
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null);

        void Write(
            LogLevel level,
            string value = null,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null);
    }
}
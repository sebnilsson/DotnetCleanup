namespace DotnetCleanup
{
    public static class LogExtensions
    {
        public static void Debug(
            this ILog log,
            string value = null)
        {
            log.Write(LogLevel.Detailed, value, ConsoleColors.Debug);
        }

        public static void Detailed(
            this ILog log,
            string value = null,
            ConsoleColors.Profile colorProfile = null)
        {
            log.Write(LogLevel.Detailed, value, colorProfile);
        }

        public static void Minimal(
            this ILog log,
            string value = null,
            ConsoleColors.Profile colorProfile = null)
        {
            log.Write(LogLevel.Minimal, value, colorProfile);
        }

        public static void Normal(
            this ILog log,
            string value = null,
            ConsoleColors.Profile colorProfile = null)
        {
            log.Write(LogLevel.Normal, value, colorProfile);
        }

        public static void Write(
            this ILog log,
            LogLevel LogLevel,
            string value = null,
            ConsoleColors.Profile colorProfile = null)
        {
            log.Write(
                LogLevel,
                value,
                colorProfile?.Foreground,
                colorProfile?.Background);
        }
    }
}
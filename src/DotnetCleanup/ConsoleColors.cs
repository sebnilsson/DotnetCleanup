using System;

namespace DotnetCleanup
{
    public static class ConsoleColors
    {
        public static Profile Debug = new Profile
        {
            Foreground = ConsoleColor.Magenta
        };

        public static Profile Error = new Profile
        {
            Foreground = ConsoleColor.Red
        };

        public static Profile Info = new Profile
        {
            Foreground = ConsoleColor.Cyan
        };

        public static Profile Success = new Profile
        {
            Foreground = ConsoleColor.Green
        };

        public static Profile Warning = new Profile
        {
            Foreground = ConsoleColor.Yellow
        };

        public class Profile
        {
            public ConsoleColor? Background { get; set; }

            public ConsoleColor? Foreground { get; set; }
        }
    }
}
using System.IO;

namespace DotnetCleanup
{
    internal static class PathUtility
    {
        private static readonly char SeparatorChar =
            Path.DirectorySeparatorChar;

        public static string GetCleanPath(string path)
        {
            return path?.Replace('\\', SeparatorChar)
                    .Replace('/', SeparatorChar)
                    .TrimStart(SeparatorChar);
        }

        public static string GetParentPath(string path)
        {
            var directoryIndex =
                path?.LastIndexOf(Path.DirectorySeparatorChar)
                ?? -1;

            if (directoryIndex < 0)
            {
                return null;
            }

            return path.Substring(0, directoryIndex);
        }
    }
}

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
                    .Replace('/', SeparatorChar);
        }
    }
}

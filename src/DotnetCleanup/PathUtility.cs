namespace DotnetCleanup
{
    internal static class PathUtility
    {
        private static readonly char s_separatorChar =
            Path.DirectorySeparatorChar;

        public static string? GetCleanPath(string? path)
        {
            return path?.Replace('\\', s_separatorChar)
                    .Replace('/', s_separatorChar)
                    .TrimStart(s_separatorChar);
        }

        public static string? GetParentPath(string? path)
        {
            var directoryIndex =
                path?.LastIndexOf(Path.DirectorySeparatorChar)
                ?? -1;

            if (directoryIndex < 0)
            {
                return null;
            }

            return path?.Substring(0, directoryIndex);
        }
    }
}

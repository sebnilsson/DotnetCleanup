namespace DotnetCleanup.IO;

public static class PathUtility
{
    private static readonly char s_directorySeparatorChar = Path.DirectorySeparatorChar;

    public static string CreateTempDirectoryName(DateTimeOffset dateTime) => $"~dotnetcleanup-{dateTime:yyyyMMdd-HHmmss}";

    public static string? GetNormalizedPath(string? path)
    {
        var normalized = path?.Replace('\\', s_directorySeparatorChar)
                .Replace('/', s_directorySeparatorChar);

        return !string.IsNullOrWhiteSpace(normalized) ? Path.TrimEndingDirectorySeparator(normalized) : null;
    }

    public static string? GetParentPath(string? path)
    {
        var directoryIndex =
            path?.LastIndexOf(s_directorySeparatorChar)
            ?? -1;

        if (directoryIndex < 0)
        {
            return null;
        }

        return path?[..directoryIndex];
    }

    public static string? GetRelativePath(string rootPath, string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            ? Path.GetRelativePath(rootPath, path).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')
            : null;
    }
}

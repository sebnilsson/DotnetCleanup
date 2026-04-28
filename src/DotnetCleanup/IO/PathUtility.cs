namespace DotnetCleanup.IO;

public static class PathUtility
{
    private static readonly char s_directorySeparatorChar = Path.DirectorySeparatorChar;

    public static string? GetNormalizedPath(string? path)
    {
        var normalized = path is not null ? NormalizeDirectorySeparators(path) : null;

        return !string.IsNullOrWhiteSpace(normalized) ? Path.TrimEndingDirectorySeparator(normalized) : null;
    }

    public static string? GetParentPath(string? path)
    {
        var normalizedPath = GetNormalizedPath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        return Path.GetDirectoryName(normalizedPath);
    }

    public static string? GetRelativePath(string rootPath, string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            ? Path.GetRelativePath(NormalizeDirectorySeparators(rootPath), NormalizeDirectorySeparators(path))
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/')
            : null;
    }

    private static string NormalizeDirectorySeparators(string path)
    {
        return path.Replace('\\', s_directorySeparatorChar)
            .Replace('/', s_directorySeparatorChar);
    }
}

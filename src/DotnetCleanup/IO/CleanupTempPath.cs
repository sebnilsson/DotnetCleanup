namespace DotnetCleanup.IO;

public static class CleanupTempPath
{
    public const string DirectoryNamePrefix = "~dotnetcleanup";

    public static string CreateRunDirectoryPath(string tempPath, DateTimeOffset startedAt)
    {
        return CreatePath(tempPath, $"{DirectoryNamePrefix}-{startedAt:yyyyMMdd-HHmmss}-{Guid.CreateVersion7():N}");
    }

    public static string GetRunDirectoryPrefix(string tempPath, DateTimeOffset startedAt)
    {
        return CreatePath(tempPath, $"{DirectoryNamePrefix}-{startedAt:yyyyMMdd-HHmmss}-");
    }

    public static string CreatePath(string tempPath, string directoryName, params string[] segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);

        var path = Path.Combine(tempPath, directoryName);

        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        return PathUtility.GetNormalizedPath(path) ?? string.Empty;
    }
}

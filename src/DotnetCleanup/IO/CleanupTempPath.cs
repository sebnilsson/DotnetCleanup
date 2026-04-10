namespace DotnetCleanup.IO;

internal static class CleanupTempPath
{
    internal const string DirectoryNamePrefix = "~dotnetcleanup";

    internal static string CreateRunDirectoryPath(string tempPath, DateTimeOffset startedAt)
    {
        return CreatePath(tempPath, $"{DirectoryNamePrefix}-{startedAt:yyyyMMdd-HHmmss}-{Guid.CreateVersion7():N}");
    }

    internal static string GetRunDirectoryPrefix(string tempPath, DateTimeOffset startedAt)
    {
        return CreatePath(tempPath, $"{DirectoryNamePrefix}-{startedAt:yyyyMMdd-HHmmss}-");
    }

    internal static string CreatePath(string tempPath, string directoryName, params string[] segments)
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

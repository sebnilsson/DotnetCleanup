using DotnetCleanup.IO;

namespace DotnetCleanup.Tests.IO;

public static class TestPath
{
    public static StringComparer PathComparer { get; } = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static StringComparison PathComparison { get; } = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static string RootPath { get; } = GetRootedPath("root-path");

    public static string TempPath { get; } = GetRootedPath("temp-path");

    public static string CurrentDirectory { get; } = GetRootedPath("InMemoryCurrentDirectory");

    public static string SystemTempPath { get; } = GetRootedPath("InMemoryTempPath");

    public static string Root(params string[] segments) => Combine(RootPath, segments);

    public static string Temp(params string[] segments) => Combine(TempPath, segments);

    public static string Combine(string path, params string[] segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var combinedPath = path;

        foreach (var segment in segments)
        {
            combinedPath = Path.Combine(combinedPath, segment);
        }

        return PathUtility.GetNormalizedPath(combinedPath) ?? string.Empty;
    }

    private static string GetRootedPath(string name)
    {
        var root = Path.GetPathRoot(Environment.CurrentDirectory)
            ?? throw new InvalidOperationException("Current directory must be rooted.");

        return PathUtility.GetNormalizedPath(Path.Combine(root, name)) ?? string.Empty;
    }
}

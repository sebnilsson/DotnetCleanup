using DotnetCleanup.IO;

namespace DotnetCleanup;

public struct PathInfo(string value, bool isFile)
{
    public Exception? Exception { get; set; }

    public readonly bool IsFile { get; } = isFile;

    public readonly string Normalized { get; } = PathUtility.GetNormalizedPath(value) ?? string.Empty;

    public readonly string Value { get; } = value;

    public override readonly int GetHashCode()
    {
        return Normalized.GetHashCode();
    }

    public void SetException(Exception exception)
    {
        Exception = exception;
    }
}

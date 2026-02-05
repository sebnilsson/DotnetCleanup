using System.Diagnostics;
using DotnetCleanup.IO;

namespace DotnetCleanup;

[DebuggerDisplay("{Value} (IsFile: {IsFile}, Exception: {Exception != null})")]
public struct PathInfo(string value, bool isFile)
{
    public Exception? Exception { get; set; }

    public readonly bool IsFile { get; } = isFile;

    public readonly string Parent { get; } = PathUtility.GetParentPath(PathUtility.GetNormalizedPath(value)) ?? string.Empty;

    public readonly string Value { get; } = PathUtility.GetNormalizedPath(value) ?? string.Empty;

    public readonly string Raw { get; } = value;

    public override readonly int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public void SetException(Exception exception)
    {
        Exception = exception;
    }
}

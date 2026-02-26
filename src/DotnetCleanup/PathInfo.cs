using System.Diagnostics;
using DotnetCleanup.IO;

namespace DotnetCleanup;

[DebuggerDisplay("{Value} (IsFile: {IsFile}, FailedOn: {FailedOn}, Exception: {Exception != null})")]
public sealed class PathInfo
{
    public PathInfo(string value, bool isFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalizedPath = PathUtility.GetNormalizedPath(value)
            ?? throw new ArgumentException("Path value must resolve to a non-empty normalized path.", nameof(value));

        Raw = value;
        InitialValue = normalizedPath;
        Value = normalizedPath;
        IsFile = isFile;
    }

    public Exception? Exception { get; private set; }

    public PathFailureStage? FailedOn { get; private set; }

    public bool IsFile { get; }

    public string MovePath { get; private set; } = string.Empty;

    public string Parent => PathUtility.GetParentPath(Value) ?? string.Empty;

    public string Value { get; }

    public string InitialValue { get; }

    public string Raw { get; }

    public void SetMovePath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        MovePath = PathUtility.GetNormalizedPath(value)
            ?? throw new ArgumentException("Move path must resolve to a non-empty normalized path.", nameof(value));
    }

    public void SetFailedOnList(Exception exception)
    {
        SetFailed(exception, PathFailureStage.List);
    }

    public void SetFailedOnMove(Exception exception)
    {
        SetFailed(exception, PathFailureStage.Move);
    }

    public void SetFailedOnDelete(Exception exception)
    {
        SetFailed(exception, PathFailureStage.Delete);
    }

    private void SetFailed(Exception exception, PathFailureStage failedOn)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception = exception;
        FailedOn = failedOn;
    }
}

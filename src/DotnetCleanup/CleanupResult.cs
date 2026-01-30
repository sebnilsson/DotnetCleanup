namespace DotnetCleanup;

internal sealed class CleanupResult
{
    public int SuccessCount { get; private set; }

    public int ErrorCount { get; private set; }

    public void AddSuccess() => SuccessCount++;

    public void AddError()
    {
        ErrorCount++;
    }
}

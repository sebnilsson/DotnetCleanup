namespace DotnetCleanup;

public sealed class CleanupResult
{
    public CleanupStep GetStep { get; } = new();
    public CleanupStep MoveStep { get; } = new();
    public CleanupStep DeleteStep { get; } = new();
}

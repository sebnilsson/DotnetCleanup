namespace DotnetCleanup;

public sealed record CleanupResult
{
    public CleanupStep GetStep { get; } = new CleanupStep();
    public CleanupStep MoveStep { get; } = new CleanupStep();
    public CleanupStep DeleteStep { get; } = new CleanupStep();
}

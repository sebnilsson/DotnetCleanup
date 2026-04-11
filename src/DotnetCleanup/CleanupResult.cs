namespace DotnetCleanup;

public sealed class CleanupResult
{
    public CleanupStep? ListStep { get; internal set; }

    public CleanupStep? MoveStep { get; internal set; }

    public CleanupStep? DeleteStep { get; internal set; }
}

namespace DotnetCleanup;

public sealed class CleanupResult
{
    public CleanupStep ListStep { get; } = new();
    public CleanupStep MoveStep { get; } = new();
    public CleanupStep DeleteStep { get; } = new();
    public CleanupStage LastExecutedStage { get; private set; } = CleanupStage.List;

    public CleanupStep LastExecutedStep => LastExecutedStage switch
    {
        CleanupStage.List => ListStep,
        CleanupStage.Move => MoveStep,
        CleanupStage.Delete => DeleteStep,
        _ => ListStep
    };

    internal void MarkLastExecutedStage(CleanupStage stage)
    {
        LastExecutedStage = stage;
    }
}

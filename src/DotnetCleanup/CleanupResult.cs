namespace DotnetCleanup;

public sealed record CleanupResult(
    CleanupStep GetStep,
    CleanupStep? MoveStep = default,
    CleanupStep? DeleteStep = default);

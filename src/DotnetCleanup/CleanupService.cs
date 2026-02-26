using DotnetCleanup.Cli;
using DotnetCleanup.IO;

namespace DotnetCleanup;

public sealed class CleanupService(FileSystemService fileSystemService)
{
    public delegate void PathHandler(PathInfo path);

    public delegate void StepDoneHandler(CleanupStep stepResult);

    public delegate void StepStartHandler();

    public event PathHandler? OnListPath;

    public event PathHandler? OnMovePath;

    public event PathHandler? OnDeletePath;

    public event StepStartHandler? OnListPathsStepStart;

    public event StepStartHandler? OnMovePathsStepStart;

    public event StepStartHandler? OnDeletePathsStepStart;

    public event StepDoneHandler? OnListPathsStepDone;

    public event StepDoneHandler? OnMovePathsStepDone;

    public event StepDoneHandler? OnDeletePathsStepDone;

    public CleanupResult Cleanup(Func<bool> onConfirmCallback, CleanupSettings settings, CancellationToken cancellationToken)
    {
        fileSystemService.ValidateSettings(settings);

        var cleanupResult = new CleanupResult();

        ListPaths(cleanupResult, settings, cancellationToken);

        var isConfirmed = onConfirmCallback();
        if (!isConfirmed)
        {
            return cleanupResult;
        }

        var tempPath = settings.Noop ? string.Empty : fileSystemService.EnsureTempDirectory(settings);

        MovePaths(cleanupResult, tempPath, settings, cancellationToken);

        DeletePaths(cleanupResult, settings, cancellationToken);

        return cleanupResult;
    }

    private void ListPaths(CleanupResult cleanupResult, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnListPathsStepStart?.Invoke();

        foreach (var path in fileSystemService.GetPaths(settings, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddPath(cleanupResult.GetStep, path, OnListPath);
        }

        OnListPathsStepDone?.Invoke(cleanupResult.GetStep);
    }

    private void MovePaths(CleanupResult cleanupResult, string tempPath, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnMovePathsStepStart?.Invoke();

        if (settings.Noop)
        {
            OnMovePathsStepDone?.Invoke(cleanupResult.MoveStep);
            return;
        }

        if (settings.SkipMove)
        {
            foreach (var path in cleanupResult.GetStep.Successes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AddPath(cleanupResult.MoveStep, path, OnMovePath);
            }
        }
        else
        {
            Parallel.ForEach(cleanupResult.GetStep.Successes, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var movePath = fileSystemService.MovePath(tempPath, path, settings);
                AddPath(cleanupResult.MoveStep, movePath, OnMovePath);
            });
        }

        OnMovePathsStepDone?.Invoke(cleanupResult.MoveStep);
    }

    private void DeletePaths(CleanupResult cleanupResult, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnDeletePathsStepStart?.Invoke();

        if (!settings.ShouldSkipDelete())
        {
            Parallel.ForEach(cleanupResult.MoveStep.Successes, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var deletePath = fileSystemService.DeletePath(path);

                AddPath(cleanupResult.DeleteStep, deletePath, OnDeletePath);
            });
        }

        OnDeletePathsStepDone?.Invoke(cleanupResult.DeleteStep);
    }

    private static void AddPath(CleanupStep step, PathInfo path, PathHandler? pathEventHandler)
    {
        var isAdded = path.Exception == null
            ? step.AddSuccess(path)
            : step.AddFailed(path);

        if (isAdded)
        {
            pathEventHandler?.Invoke(path);
        }
    }
}

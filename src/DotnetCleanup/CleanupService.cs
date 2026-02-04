using DotnetCleanup.Cli;
using DotnetCleanup.IO;

namespace DotnetCleanup;

public sealed class CleanupService(FileSystemService fileSystemService)
{
    public delegate void PathHandler(PathInfo path);

    public delegate void StepDoneHandler(CleanupStep stepResult);

    public delegate void StepStartHandler();

    public event PathHandler? OnGetPath;

    public event PathHandler? OnMovePath;

    public event PathHandler? OnDeletePath;

    public event StepStartHandler? OnGetPathsStepStart;

    public event StepStartHandler? OnMovePathsStepStart;

    public event StepStartHandler? OnDeletePathsStepStart;

    public event StepDoneHandler? OnGetPathsStepDone;

    public event StepDoneHandler? OnMovePathsStepDone;

    public event StepDoneHandler? OnDeletePathsStepDone;

    public CleanupResult Cleanup(Func<bool> onConfirmCallback, CleanupSettings settings, CancellationToken cancellationToken)
    {
        fileSystemService.ValidateSettings(settings);

        var pathsResult = GetPaths(settings, cancellationToken);

        var isConfirmed = onConfirmCallback();
        if (!isConfirmed)
        {
            return new CleanupResult(pathsResult);
        }

        var moveResult = MovePaths(pathsResult.Successes, settings, cancellationToken);

        var deleteResult = DeletePaths(moveResult.Successes, settings, cancellationToken);

        return new CleanupResult(pathsResult, moveResult, deleteResult);
    }

    private CleanupStep GetPaths(CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnGetPathsStepStart?.Invoke();

        CleanupStep step = new();

        foreach (var path in fileSystemService.GetPaths(settings, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isAdded = path.Exception != null ? step.Successes.Add(path) : step.Errors.Add(path);
            if (isAdded)
            {
                OnGetPath?.Invoke(path);
            }
        }

        OnGetPathsStepDone?.Invoke(step);

        return step;
    }

    private CleanupStep MovePaths(ISet<PathInfo> paths, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnMovePathsStepStart?.Invoke();

        CleanupStep step = new();

        if (!settings.SkipMove)
        {
            Parallel.ForEach(paths, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var movePath = fileSystemService.MovePath(path, settings);

                var isAdded = movePath.Exception != null ? step.Successes.Add(movePath) : step.Errors.Add(movePath);
                if (isAdded)
                {
                    OnMovePath?.Invoke(movePath);
                }
            });
        }

        OnMovePathsStepDone?.Invoke(step);

        return step;
    }

    private CleanupStep DeletePaths(ISet<PathInfo> deletePaths, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnDeletePathsStepStart?.Invoke();

        CleanupStep step = new();

        if (!settings.SkipDelete)
        {
            Parallel.ForEach(deletePaths, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var deletePath = fileSystemService.DeletePath(path);

                var isAdded = deletePath.Exception != null ? step.Successes.Add(deletePath) : step.Errors.Add(deletePath);
                if (isAdded)
                {
                    OnDeletePath?.Invoke(deletePath);
                }
            });
        }

        OnDeletePathsStepDone?.Invoke(step);

        return step;
    }
}

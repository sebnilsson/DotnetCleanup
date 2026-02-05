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

        var listResult = ListPaths(settings, cancellationToken);

        var isConfirmed = onConfirmCallback();
        if (!isConfirmed)
        {
            return new CleanupResult(listResult);
        }

        var tempPath = fileSystemService.EnsureTempDirectory(settings);

        var moveResult = MovePaths(tempPath, listResult.Successes, settings, cancellationToken);

        var deleteResult = DeletePaths(tempPath, moveResult.Successes, settings, cancellationToken);

        return new CleanupResult(listResult, moveResult, deleteResult);
    }

    private CleanupStep ListPaths(CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnListPathsStepStart?.Invoke();

        CleanupStep step = new();

        foreach (var path in fileSystemService.GetPaths(settings, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isAdded = path.Exception == null ? step.AddSuccess(path) : step.AddError(path);
            if (isAdded)
            {
                OnListPath?.Invoke(path);
            }
        }

        OnListPathsStepDone?.Invoke(step);

        return step;
    }

    private CleanupStep MovePaths(string tempPath, ISet<PathInfo> paths, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnMovePathsStepStart?.Invoke();

        CleanupStep step = new();

        if (!settings.SkipMove)
        {
            Parallel.ForEach(paths, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var movePath = fileSystemService.MovePath(tempPath, path, settings);

                var isAdded = movePath.Exception == null ? step.AddSuccess(movePath) : step.AddError(movePath);
                if (isAdded)
                {
                    OnMovePath?.Invoke(movePath);
                }
            });
        }

        OnMovePathsStepDone?.Invoke(step);

        return step;
    }

    private CleanupStep DeletePaths(string tempPath, ISet<PathInfo> deletePaths, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnDeletePathsStepStart?.Invoke();

        CleanupStep step = new();

        if (!settings.SkipDelete)
        {
            if (settings.SkipMove)
            {
                Parallel.ForEach(deletePaths, path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var deletePath = fileSystemService.DeletePath(path);

                    var isAdded = deletePath.Exception == null ? step.AddSuccess(deletePath) : step.AddError(deletePath);
                    if (isAdded)
                    {
                        OnDeletePath?.Invoke(deletePath);
                    }
                });
            }
            else
            {
                var temp = new PathInfo(tempPath, isFile: false);

                var deletePath = fileSystemService.DeletePath(temp);

                var isAdded = deletePath.Exception == null ? step.AddSuccess(deletePath) : step.AddError(deletePath);
                if (isAdded)
                {
                    OnDeletePath?.Invoke(deletePath);
                }
            }
        }

        OnDeletePathsStepDone?.Invoke(step);

        return step;
    }
}

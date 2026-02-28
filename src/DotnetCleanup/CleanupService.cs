using DotnetCleanup.Cli;
using DotnetCleanup.IO;

namespace DotnetCleanup;

public sealed class CleanupService(IFileSystem fileSystem)
{
    private readonly FileSystemService _fileSystemService = new(fileSystem);

    public event Action<PathInfo>? OnListPath;

    public event Action<PathInfo>? OnMovePath;

    public event Action<PathInfo>? OnDeletePath;

    public event Action? OnListPathsStepStart;

    public event Action? OnMovePathsStepStart;

    public event Action? OnDeletePathsStepStart;

    public event Action<CleanupStep>? OnListPathsStepDone;

    public event Action<CleanupStep>? OnMovePathsStepDone;

    public event Action<CleanupStep>? OnDeletePathsStepDone;

    public CleanupResult Cleanup(Func<bool> onConfirmCallback, CleanupSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onConfirmCallback);
        ArgumentNullException.ThrowIfNull(settings);

        _fileSystemService.ValidateSettings(settings);

        var cleanupResult = new CleanupResult();

        ListPaths(cleanupResult, settings, cancellationToken);

        if (!onConfirmCallback())
        {
            return cleanupResult;
        }

        var tempPath = settings.ShouldSkipMove() ? string.Empty : _fileSystemService.EnsureTempDirectory(settings);

        MovePaths(cleanupResult, tempPath, settings, cancellationToken);

        DeletePaths(cleanupResult, settings, cancellationToken);

        return cleanupResult;
    }

    private void ListPaths(CleanupResult cleanupResult, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnListPathsStepStart?.Invoke();

        foreach (var path in _fileSystemService.GetPaths(settings, cancellationToken))
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
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };

            Parallel.ForEach(cleanupResult.GetStep.Successes, parallelOptions, path =>
            {
                var movePath = _fileSystemService.MovePath(tempPath, path, settings);
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
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };

            Parallel.ForEach(cleanupResult.MoveStep.Successes, parallelOptions, path =>
            {
                var deletePath = _fileSystemService.DeletePath(path);

                AddPath(cleanupResult.DeleteStep, deletePath, OnDeletePath);
            });
        }

        OnDeletePathsStepDone?.Invoke(cleanupResult.DeleteStep);
    }

    private static void AddPath(CleanupStep step, PathInfo path, Action<PathInfo>? pathEventHandler)
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

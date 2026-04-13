using DotnetCleanup.Cli;
using DotnetCleanup.IO;

namespace DotnetCleanup;

public sealed class CleanupService(IFileSystem fileSystem)
{
    private const int MaxParallelism = 4;
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

    public CleanupResult Cleanup(Func<bool> onConfirm, CleanupSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onConfirm);
        ArgumentNullException.ThrowIfNull(settings);

        _fileSystemService.ValidateSettings(settings);

        var cleanupResult = new CleanupResult();

        ListPaths(cleanupResult, settings, cancellationToken);

        if (!onConfirm())
        {
            return cleanupResult;
        }

        var tempPath = settings.ShouldSkipMove() ? string.Empty : _fileSystemService.EnsureTempDirectory(settings);

        MovePaths(cleanupResult, tempPath, settings, cancellationToken);

        DeletePaths(cleanupResult, tempPath, settings, cancellationToken);

        return cleanupResult;
    }

    private void ListPaths(CleanupResult cleanupResult, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnListPathsStepStart?.Invoke();
        cleanupResult.ListStep = new CleanupStep();
        var listStep = cleanupResult.ListStep;

        foreach (var path in _fileSystemService.GetPaths(settings, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddPath(listStep, path, OnListPath);
        }

        OnListPathsStepDone?.Invoke(listStep);
    }

    private void MovePaths(CleanupResult cleanupResult, string tempPath, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnMovePathsStepStart?.Invoke();

        if (settings.Noop)
        {
            return;
        }

        cleanupResult.MoveStep = new CleanupStep();
        var moveStep = cleanupResult.MoveStep;
        var listStep = cleanupResult.ListStep ?? throw new InvalidOperationException("List step must run before move step.");

        if (settings.SkipMove)
        {
            foreach (var path in listStep.Successes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AddPath(moveStep, path, OnMovePath);
            }
        }
        else
        {
            Parallel.ForEach(listStep.Successes, CreateParallelOptions(cancellationToken), path =>
            {
                var movePath = _fileSystemService.MovePath(tempPath, path, settings);
                AddPath(moveStep, movePath, OnMovePath);
            });
        }

        OnMovePathsStepDone?.Invoke(moveStep);
    }

    private void DeletePaths(CleanupResult cleanupResult, string tempPath, CleanupSettings settings, CancellationToken cancellationToken)
    {
        OnDeletePathsStepStart?.Invoke();

        if (settings.ShouldSkipDelete())
        {
            return;
        }

        cleanupResult.DeleteStep = new CleanupStep();
        var deleteStep = cleanupResult.DeleteStep;
        var moveStep = cleanupResult.MoveStep ?? throw new InvalidOperationException("Move step must run before delete step.");

        if (settings.ShouldSkipMove())
        {
            Parallel.ForEach(moveStep.Successes, CreateParallelOptions(cancellationToken), path =>
            {
                var deletePath = _fileSystemService.DeletePath(path);

                AddPath(deleteStep, deletePath, OnDeletePath);
            });
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deletePath = _fileSystemService.DeletePath(new PathInfo(tempPath, isFile: false));
            AddPath(deleteStep, deletePath, OnDeletePath);
        }

        OnDeletePathsStepDone?.Invoke(deleteStep);
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

    private static ParallelOptions CreateParallelOptions(CancellationToken cancellationToken)
    {
        return new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, MaxParallelism)
        };
    }
}

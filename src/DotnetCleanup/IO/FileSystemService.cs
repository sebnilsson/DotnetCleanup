using DotnetCleanup.Cli;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DotnetCleanup.IO;

public class FileSystemService(IFileSystem fileSystem)
{
    private static readonly Lock s_createDirectoryLock = new();

    public IEnumerable<PathInfo> GetPaths(CleanupSettings settings, CancellationToken cancellationToken)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(settings.Include);
        matcher.AddExcludePatterns(settings.Exclude);

        return GetPathsInternal(settings.Path, new PathInfo(settings.Path, isFile: false), matcher, cancellationToken);
    }

    public PathInfo MovePath(string tempPath, PathInfo path, CleanupSettings settings)
    {
        var relativePath = PathUtility.GetRelativePath(settings.Path, path.Value);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            path.SetFailedOnMove(new ArgumentOutOfRangeException(nameof(path), $"Failed to resolve relative path for the given path: {path.Value}"));
            return path;
        }

        var targetPath = PathUtility.GetNormalizedPath(Path.Combine(tempPath, relativePath)) ?? string.Empty;
        var targetParent = PathUtility.GetParentPath(targetPath) ?? string.Empty;

        try
        {
            EnsureDirectory(targetParent);

            if (path.IsFile)
            {
                fileSystem.MoveFile(path.Value, targetPath);
            }
            else
            {
                fileSystem.MoveDirectory(path.Value, targetPath);
            }

            path.SetMovePath(targetPath);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException)
        {
            path.SetFailedOnMove(ex);
        }

        return path;
    }

    public PathInfo DeletePath(PathInfo path)
    {
        try
        {
            var deletePath = !string.IsNullOrWhiteSpace(path.MovePath) ? path.MovePath : path.Value;

            if (path.IsFile)
            {
                fileSystem.DeleteFile(deletePath);
            }
            else
            {
                fileSystem.DeleteDirectory(deletePath);
            }
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException)
        {
            path.SetFailedOnDelete(ex);
        }

        return path;
    }

    public string EnsureTempDirectory(CleanupSettings settings)
    {
        var path = Path.Combine(settings.TempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");

        if (!fileSystem.DirectoryExists(path))
        {
            fileSystem.CreateDirectory(path);
        }

        return path;
    }

    public void ValidateSettings(CleanupSettings settings)
    {
        if (settings.Include.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "At least one include pattern must be specified.");
        }
        if (!fileSystem.DirectoryExists(settings.Path))
        {
            throw new DirectoryNotFoundException($"The given path does not exist: {settings.Path}");
        }
        if (!fileSystem.DirectoryExists(settings.TempPath))
        {
            throw new DirectoryNotFoundException($"The given temporary path does not exist: {settings.TempPath}");
        }
    }

    private void EnsureDirectory(string path)
    {
        if (!fileSystem.DirectoryExists(path))
        {
            lock (s_createDirectoryLock)
            {
                if (!fileSystem.DirectoryExists(path))
                {
                    fileSystem.CreateDirectory(path);
                }
            }
        }
    }

    private IEnumerable<PathInfo> GetPathsInternal(string rootDirectory, PathInfo path, Matcher matcher, CancellationToken cancellationToken)
    {
        if (!TryEnumerateFiles(path.Value, out var files, out var fileException))
        {
            path.SetFailedOnList(fileException!);
            yield return path;
            yield break;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchPath(rootDirectory, file, matcher))
            {
                yield return new PathInfo(file, isFile: true);
            }
        }

        if (!TryEnumerateDirectories(path.Value, out var directories, out var directoryException))
        {
            path.SetFailedOnList(directoryException!);
            yield return path;
            yield break;
        }

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchPath(rootDirectory, directory, matcher))
            {
                yield return new PathInfo(directory, isFile: false);
            }
            else
            {
                var subDirectory = new PathInfo(directory, isFile: false);
                foreach (var subPath in GetPathsInternal(rootDirectory, subDirectory, matcher, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    yield return subPath;
                }
            }
        }
    }

    private static bool MatchPath(string rootPath, string path, Matcher matcher)
    {
        var relativePath = PathUtility.GetRelativePath(rootPath, path);
        var result = !string.IsNullOrWhiteSpace(relativePath) ? matcher.Match(relativePath) : null;
        return result?.HasMatches ?? false;
    }

    private static bool IsPathEnumerationException(Exception exception)
    {
        return exception is UnauthorizedAccessException ||
            exception is IOException ||
            exception is DirectoryNotFoundException;
    }

    private bool TryEnumerateFiles(string path, out IEnumerable<string> files, out Exception? exception)
    {
        try
        {
            files = fileSystem.EnumerateFiles(path);
            exception = null;
            return true;
        }
        catch (Exception ex) when (IsPathEnumerationException(ex))
        {
            files = [];
            exception = ex;
            return false;
        }
    }

    private bool TryEnumerateDirectories(string path, out IEnumerable<string> directories, out Exception? exception)
    {
        try
        {
            directories = fileSystem.EnumerateDirectories(path);
            exception = null;
            return true;
        }
        catch (Exception ex) when (IsPathEnumerationException(ex))
        {
            directories = [];
            exception = ex;
            return false;
        }
    }
}

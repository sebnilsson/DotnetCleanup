using System.Diagnostics.CodeAnalysis;
using DotnetCleanup.Cli;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DotnetCleanup.IO;

public sealed class FileSystemService(IFileSystem fileSystem)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    public IEnumerable<PathInfo> GetPaths(CleanupSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(settings.Include);
        matcher.AddExcludePatterns(settings.Exclude);

        return GetPathsInternal(settings.Path, new PathInfo(settings.Path, isFile: false), matcher, cancellationToken);
    }

    public PathInfo MovePath(string tempPath, PathInfo path, CleanupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(settings);

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
                _fileSystem.MoveFile(path.Value, targetPath);
            }
            else
            {
                _fileSystem.MoveDirectory(path.Value, targetPath);
            }

            path.SetMovePath(targetPath);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException ||
            ex is DirectoryNotFoundException ||
            ex is FileNotFoundException)
        {
            path.SetFailedOnMove(ex);
        }

        return path;
    }

    public PathInfo DeletePath(PathInfo path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            var deletePath = !string.IsNullOrWhiteSpace(path.MovePath) ? path.MovePath : path.Value;

            if (path.IsFile)
            {
                _fileSystem.DeleteFile(deletePath);
            }
            else
            {
                _fileSystem.DeleteDirectory(deletePath);
            }
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException ||
            ex is DirectoryNotFoundException ||
            ex is FileNotFoundException)
        {
            path.SetFailedOnDelete(ex);
        }

        return path;
    }

    public string EnsureTempDirectory(CleanupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var path = CleanupTempPath.CreateRunDirectoryPath(settings.TempPath, settings.StartedAt);

        _fileSystem.CreateDirectory(path);

        return path;
    }

    public void ValidateSettings(CleanupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Include.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "At least one include pattern must be specified.");
        }
        if (!_fileSystem.DirectoryExists(settings.Path))
        {
            throw new DirectoryNotFoundException($"The given path does not exist: {settings.Path}");
        }
        if (!settings.ShouldSkipMove() && !_fileSystem.DirectoryExists(settings.TempPath))
        {
            throw new DirectoryNotFoundException($"The given temporary path does not exist: {settings.TempPath}");
        }
    }

    private void EnsureDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _fileSystem.CreateDirectory(path);
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

        using var fileEnumerator = files.GetEnumerator();

        while (TryMoveNext(fileEnumerator, out var file, out fileException))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchPath(rootDirectory, file, matcher))
            {
                yield return new PathInfo(file, isFile: true);
            }
        }

        if (fileException != null)
        {
            path.SetFailedOnList(fileException);
            yield return path;
            yield break;
        }

        if (!TryEnumerateDirectories(path.Value, out var directories, out var directoryException))
        {
            path.SetFailedOnList(directoryException!);
            yield return path;
            yield break;
        }

        using var directoryEnumerator = directories.GetEnumerator();

        while (TryMoveNext(directoryEnumerator, out var directory, out directoryException))
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

        if (directoryException != null)
        {
            path.SetFailedOnList(directoryException);
            yield return path;
        }
    }

    private static bool MatchPath(string rootPath, string path, Matcher matcher)
    {
        var relativePath = PathUtility.GetRelativePath(rootPath, path);
        return !string.IsNullOrWhiteSpace(relativePath) && matcher.Match(relativePath).HasMatches;
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
            files = _fileSystem.EnumerateFiles(path);
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
            directories = _fileSystem.EnumerateDirectories(path);
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

    private static bool TryMoveNext(IEnumerator<string> enumerator, [NotNullWhen(true)] out string? current, out Exception? exception)
    {
        try
        {
            if (!enumerator.MoveNext())
            {
                current = null;
                exception = null;
                return false;
            }

            current = enumerator.Current;
            exception = null;
            return true;
        }
        catch (Exception ex) when (IsPathEnumerationException(ex))
        {
            current = null;
            exception = ex;
            return false;
        }
    }
}

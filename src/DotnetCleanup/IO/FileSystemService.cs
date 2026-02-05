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
            throw new ArgumentOutOfRangeException(nameof(path), $"Failed to resolve relative path for the given path: {path.Value}");
        }

        var targetPath = Path.Combine(tempPath, relativePath);
        var movePath = new PathInfo(targetPath, path.IsFile);

        try
        {
            if (path.IsFile)
            {
                EnsureDirectory(movePath.Parent);

                fileSystem.MoveFile(path.Value, movePath.Value);
            }
            else
            {
                EnsureDirectory(movePath.Parent);

                fileSystem.MoveDirectory(path.Value, movePath.Value);
            }
        }
        catch (IOException ex)
        {
            movePath.SetException(ex);
        }

        return movePath;
    }

    public PathInfo DeletePath(PathInfo path)
    {
        var delete = new PathInfo(path.Value, path.IsFile);

        try
        {
            if (delete.IsFile)
            {
                fileSystem.DeleteFile(delete.Value);
            }
            else
            {
                fileSystem.DeleteDirectory(delete.Value);
            }
        }
        catch (IOException ex)
        {
            delete.SetException(ex);
        }

        return delete;
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
        IEnumerable<string> directories;
        IEnumerable<string> files;
        try
        {
            files = fileSystem.EnumerateFiles(path.Value);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException ||
            ex is DirectoryNotFoundException)
        {
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

        try
        {
            directories = Directory.EnumerateDirectories(path.Value);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is IOException ||
            ex is DirectoryNotFoundException)
        {
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
}

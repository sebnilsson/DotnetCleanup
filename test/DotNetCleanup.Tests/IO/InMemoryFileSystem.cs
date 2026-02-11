using DotnetCleanup.IO;

namespace DotNetCleanup.Tests.IO;

public class InMemoryFileSystem(string[]? directories = null, string[]? files = null) : IFileSystem
{
    private static readonly StringComparer s_pathComparer = StringComparer.OrdinalIgnoreCase;
    private readonly Lock _fileSystemLock = new();

    public HashSet<string> Directories { get; } = CreatePathSet(directories);

    public HashSet<string> Files { get; } = CreatePathSet(files);

    public Dictionary<string, Exception> ListDirectoryExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Exception> ListFileExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Exception> MoveDirectoryExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Exception> MoveFileExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Exception> DeleteDirectoryExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Exception> DeleteFileExceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void CreateDirectory(string path)
    {
        lock (_fileSystemLock)
        {
            Directories.Add(NormalizePath(path));
        }
    }

    public void DeleteDirectory(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (DeleteDirectoryExceptions.TryGetValue(normalizedPath, out var exception))
            {
                throw exception;
            }

            var directoryPrefix = $"{normalizedPath}{Path.DirectorySeparatorChar}";

            Directories.RemoveWhere(x => s_pathComparer.Equals(x, normalizedPath) || x.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase));
            Files.RemoveWhere(x => x.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void DeleteFile(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (DeleteFileExceptions.TryGetValue(normalizedPath, out var exception))
            {
                throw exception;
            }

            Files.Remove(normalizedPath);
        }
    }

    public bool DirectoryExists(string path)
    {
        lock (_fileSystemLock)
        {
            return Directories.Contains(NormalizePath(path));
        }
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (ListFileExceptions.TryGetValue(normalizedPath, out var pathException))
            {
                throw pathException;
            }

            var files = Files.Where(x => IsDirectChild(normalizedPath, x)).ToArray();

            if (TryGetChildException(files, ListFileExceptions, out var childException))
            {
                throw childException!;
            }

            return files;
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (ListDirectoryExceptions.TryGetValue(normalizedPath, out var pathException))
            {
                throw pathException;
            }

            var directories = Directories.Where(x => IsDirectChild(normalizedPath, x)).ToArray();

            if (TryGetChildException(directories, ListDirectoryExceptions, out var childException))
            {
                throw childException!;
            }

            return directories;
        }
    }

    //public bool FileExists(string path)
    //{
    //    return Files.Contains(path);
    //}

    public string GetCurrentDirectory()
    {
        return "C:\\InMemoryCurrentDirectory";
    }

    //public bool GetIsFile(string path)
    //{
    //    return Files.Contains(path);
    //}

    public string GetTempPath()
    {
        return "C:\\InMemoryTempPath";
    }

    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        lock (_fileSystemLock)
        {
            var normalizedSourcePath = NormalizePath(sourcePath);
            if (MoveDirectoryExceptions.TryGetValue(normalizedSourcePath, out var exception))
            {
                throw exception;
            }

            var normalizedDestinationPath = NormalizePath(destinationPath);
            var sourcePrefix = $"{normalizedSourcePath}{Path.DirectorySeparatorChar}";

            var directoriesToMove = Directories
                .Where(x => s_pathComparer.Equals(x, normalizedSourcePath) || x.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (directoriesToMove.Length == 0)
            {
                return;
            }

            var filesToMove = Files
                .Where(x => x.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var directory in directoriesToMove)
            {
                Directories.Remove(directory);
            }

            foreach (var file in filesToMove)
            {
                Files.Remove(file);
            }

            foreach (var directory in directoriesToMove)
            {
                Directories.Add(ReplacePathPrefix(directory, normalizedSourcePath, normalizedDestinationPath));
            }

            foreach (var file in filesToMove)
            {
                Files.Add(ReplacePathPrefix(file, normalizedSourcePath, normalizedDestinationPath));
            }
        }
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        lock (_fileSystemLock)
        {
            var normalizedSourcePath = NormalizePath(sourcePath);
            if (MoveFileExceptions.TryGetValue(normalizedSourcePath, out var exception))
            {
                throw exception;
            }

            if (Files.Remove(normalizedSourcePath))
            {
                Files.Add(NormalizePath(destinationPath));
            }
        }
    }

    private static HashSet<string> CreatePathSet(IEnumerable<string>? paths)
    {
        HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

        if (paths != null)
        {
            foreach (var path in paths)
            {
                set.Add(NormalizePath(path));
            }
        }

        return set;
    }

    private static bool IsDirectChild(string parentPath, string candidatePath)
    {
        if (s_pathComparer.Equals(parentPath, candidatePath))
        {
            return false;
        }

        var relativePath = PathUtility.GetRelativePath(parentPath, candidatePath);
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            return false;
        }

        return relativePath.IndexOfAny(['\\', '/']) < 0;
    }

    private static string NormalizePath(string path)
    {
        return PathUtility.GetNormalizedPath(path) ?? string.Empty;
    }

    private static string ReplacePathPrefix(string value, string sourcePath, string destinationPath)
    {
        if (s_pathComparer.Equals(value, sourcePath))
        {
            return destinationPath;
        }

        return $"{destinationPath}{value[sourcePath.Length..]}";
    }

    private static bool TryGetChildException(string[] childPaths, Dictionary<string, Exception> exceptionsByPath, out Exception? exception)
    {
        foreach (var childPath in childPaths.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (exceptionsByPath.TryGetValue(childPath, out exception))
            {
                return true;
            }
        }

        exception = null;
        return false;
    }
}

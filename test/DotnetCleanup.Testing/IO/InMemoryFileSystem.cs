using DotnetCleanup.IO;

namespace DotnetCleanup.Testing.IO;

public class InMemoryFileSystem(string[]? directories = null, string[]? files = null) : IFileSystem
{
    public static string DefaultRootPath { get; } = TestPath.RootPath;

    public static string DefaultTempPath { get; } = TestPath.TempPath;

    private static readonly StringComparer s_pathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static readonly StringComparison s_pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private readonly Lock _fileSystemLock = new();

    public HashSet<string> Directories { get; } = CreatePathSet(directories ?? [DefaultRootPath, DefaultTempPath]);

    public HashSet<string> Files { get; } = CreatePathSet(files);

    public Dictionary<string, Exception> ListDirectoryExceptions { get; } = new(s_pathComparer);

    public Dictionary<string, Exception> ListFileExceptions { get; } = new(s_pathComparer);

    public Dictionary<string, Exception> MoveDirectoryExceptions { get; } = new(s_pathComparer);

    public Dictionary<string, Exception> MoveFileExceptions { get; } = new(s_pathComparer);

    public Dictionary<string, Exception> DeleteDirectoryExceptions { get; } = new(s_pathComparer);

    public Dictionary<string, Exception> DeleteFileExceptions { get; } = new(s_pathComparer);

    public virtual void CreateDirectory(string path)
    {
        lock (_fileSystemLock)
        {
            Directories.Add(NormalizePath(path));
        }
    }

    public virtual void DeleteDirectory(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (DeleteDirectoryExceptions.TryGetValue(normalizedPath, out var exception))
            {
                throw exception;
            }

            if (!Directories.Contains(normalizedPath))
            {
                throw new DirectoryNotFoundException($"Could not find a part of the path '{normalizedPath}'.");
            }

            var directoryPrefix = $"{normalizedPath}{Path.DirectorySeparatorChar}";

            Directories.RemoveWhere(x => s_pathComparer.Equals(x, normalizedPath) || x.StartsWith(directoryPrefix, s_pathComparison));
            Files.RemoveWhere(x => x.StartsWith(directoryPrefix, s_pathComparison));
        }
    }

    public virtual void DeleteFile(string path)
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

    public virtual bool DirectoryExists(string path)
    {
        lock (_fileSystemLock)
        {
            return Directories.Contains(NormalizePath(path));
        }
    }

    public virtual IEnumerable<string> EnumerateFiles(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (ListFileExceptions.TryGetValue(normalizedPath, out var pathException))
            {
                throw pathException;
            }

            var files = Files
                .Where(x => IsDirectChild(normalizedPath, x))
                .Order(s_pathComparer)
                .ToArray();

            return EnumerateChildren(files, ListFileExceptions);
        }
    }

    public virtual IEnumerable<string> EnumerateDirectories(string path)
    {
        lock (_fileSystemLock)
        {
            var normalizedPath = NormalizePath(path);
            if (ListDirectoryExceptions.TryGetValue(normalizedPath, out var pathException))
            {
                throw pathException;
            }

            var directories = Directories
                .Where(x => IsDirectChild(normalizedPath, x))
                .Order(s_pathComparer)
                .ToArray();

            return EnumerateChildren(directories, ListDirectoryExceptions);
        }
    }

    public virtual string GetCurrentDirectory()
    {
        return TestPath.CurrentDirectory;
    }

    public virtual string GetTempPath()
    {
        return TestPath.SystemTempPath;
    }

    public virtual void MoveDirectory(string sourcePath, string destinationPath)
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
                .Where(x => s_pathComparer.Equals(x, normalizedSourcePath) || x.StartsWith(sourcePrefix, s_pathComparison))
                .ToArray();

            if (directoriesToMove.Length == 0)
            {
                throw new DirectoryNotFoundException($"Could not find a part of the path '{normalizedSourcePath}'.");
            }

            var filesToMove = Files
                .Where(x => x.StartsWith(sourcePrefix, s_pathComparison))
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

    public virtual void MoveFile(string sourcePath, string destinationPath)
    {
        lock (_fileSystemLock)
        {
            var normalizedSourcePath = NormalizePath(sourcePath);
            if (MoveFileExceptions.TryGetValue(normalizedSourcePath, out var exception))
            {
                throw exception;
            }

            if (!Files.Remove(normalizedSourcePath))
            {
                throw new FileNotFoundException($"Could not find file '{normalizedSourcePath}'.", normalizedSourcePath);
            }

            Files.Add(NormalizePath(destinationPath));
        }
    }

    private static HashSet<string> CreatePathSet(IEnumerable<string>? paths)
    {
        HashSet<string> set = new(s_pathComparer);

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

    private static IEnumerable<string> EnumerateChildren(string[] childPaths, Dictionary<string, Exception> exceptionsByPath)
    {
        foreach (var childPath in childPaths)
        {
            if (exceptionsByPath.TryGetValue(childPath, out var exception))
            {
                throw exception;
            }

            yield return childPath;
        }
    }
}

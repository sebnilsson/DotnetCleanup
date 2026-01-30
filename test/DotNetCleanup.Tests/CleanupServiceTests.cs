using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupServiceTests
{
    [Fact]
    public async Task RunAsync_UsesDefaultPatternsAndRoot()
    {
        var root = CreateRoot();
        var fileSystem = new InMemoryFileSystem(root);
        fileSystem.AddDirectory(Path.Combine(root, "bin"));
        fileSystem.AddDirectory(Path.Combine(root, "src", "app", "obj"));
        fileSystem.AddDirectory(Path.Combine(root, "node_modules"));

        var console = CreateConsole();
        var logger = CreateLogger();
        var service = new CleanupService(console, fileSystem, logger);

        var settings = new CleanupSettings
        {
            ConfirmCleanup = true,
            NoMove = true,
            NoDelete = true,
            Verbosity = VerbosityLevel.Normal
        };

        var result = await service.RunAsync(settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.Contains(
            console.Lines,
            line => line.Contains(Path.Combine(root, "bin"), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            console.Lines,
            line => line.Contains(Path.Combine(root, "src", "app", "obj"), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            console.Lines,
            line => line.Contains(Path.Combine(root, "node_modules"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_ExcludePatternsWin()
    {
        var root = CreateRoot();
        var fileSystem = new InMemoryFileSystem(root);
        fileSystem.AddDirectory(Path.Combine(root, "bin"));
        fileSystem.AddDirectory(Path.Combine(root, "obj"));

        var console = CreateConsole();
        var logger = CreateLogger();
        var service = new CleanupService(console, fileSystem, logger);

        var settings = new CleanupSettings
        {
            ConfirmCleanup = true,
            NoMove = true,
            NoDelete = true,
            Verbosity = VerbosityLevel.Normal,
            Paths = ["**/bin", "**/obj"],
            Exclude = ["**/obj"]
        };

        var result = await service.RunAsync(settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.Contains(
            console.Lines,
            line => line.Contains(Path.Combine(root, "bin"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            console.Lines,
            line => line.Contains(Path.Combine(root, "obj"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_FiltersTopLevelMatches()
    {
        var root = CreateRoot();
        var fileSystem = new InMemoryFileSystem(root);
        fileSystem.AddDirectory(Path.Combine(root, "bin"));
        fileSystem.AddDirectory(Path.Combine(root, "bin", "child"));

        var console = CreateConsole();
        var logger = CreateLogger();
        var service = new CleanupService(console, fileSystem, logger);

        var settings = new CleanupSettings
        {
            ConfirmCleanup = true,
            NoMove = true,
            NoDelete = true,
            Verbosity = VerbosityLevel.Normal,
            Paths = ["**/bin", "**/bin/**"]
        };

        var result = await service.RunAsync(settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.Contains(
            console.Lines,
            line => line.Contains(Path.Combine(root, "bin"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            console.Lines,
            line => line.Contains(Path.Combine(root, "bin", "child"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_ReportsNoChangesWhenMoveAndDeleteDisabled()
    {
        var root = CreateRoot();
        var fileSystem = new InMemoryFileSystem(root);
        fileSystem.AddDirectory(Path.Combine(root, "bin"));

        var console = CreateConsole();
        var logger = CreateLogger();
        var service = new CleanupService(console, fileSystem, logger);

        var settings = new CleanupSettings
        {
            ConfirmCleanup = true,
            NoMove = true,
            NoDelete = true,
            Verbosity = VerbosityLevel.Minimal,
            Paths = ["**/bin"]
        };

        var result = await service.RunAsync(settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.Contains(
            "No changes were made because both --no-move and --no-delete are set.",
            console.Output);
    }

    [Fact]
    public async Task RunAsync_FailsWhenPathIsFile()
    {
        var root = CreateRoot();
        var fileSystem = new InMemoryFileSystem(root);
        fileSystem.AddFile(Path.Combine(root, "file.txt"));

        var console = CreateConsole();
        var logger = CreateLogger();
        var service = new CleanupService(console, fileSystem, logger);

        var settings = new CleanupSettings
        {
            Path = Path.Combine(root, "file.txt"),
            ConfirmCleanup = true
        };

        var result = await service.RunAsync(settings, CancellationToken.None);

        Assert.Equal(1, result);
        Assert.Contains("must be a directory", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    private static ILogger<CleanupService> CreateLogger()
    {
        var factory = LoggerFactory.Create(builder => { });
        return factory.CreateLogger<CleanupService>();
    }

    private static TestConsole CreateConsole()
    {
        var console = new TestConsole();
        console.Width(200);
        return console;
    }

    private static string CreateRoot() => Path.Combine(
        Path.GetTempPath(),
        "dotnetcleanup-tests",
        Guid.NewGuid().ToString("N"));

    private sealed class InMemoryFileSystem : IFileSystem
    {
        private readonly StringComparer _comparer;
        private readonly HashSet<string> _directories;
        private readonly HashSet<string> _files;
        private string _currentDirectory;

        public InMemoryFileSystem(string currentDirectory)
        {
            _comparer = OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            _directories = new HashSet<string>(_comparer);
            _files = new HashSet<string>(_comparer);
            _currentDirectory = NormalizePath(currentDirectory);
            _directories.Add(_currentDirectory);
        }

        public string GetCurrentDirectory() => _currentDirectory;

        public bool FileExists(string path) => _files.Contains(NormalizePath(path));

        public bool DirectoryExists(string path) => _directories.Contains(NormalizePath(path));

        public IEnumerable<string> EnumerateFileSystemEntries(string path, bool recursive)
        {
            var normalizedRoot = NormalizePath(path);
            var entries = _directories.Concat(_files)
                .Where(entry => IsNestedPath(entry, normalizedRoot));

            if (!recursive)
            {
                entries = entries.Where(entry => IsDirectChild(normalizedRoot, entry));
            }

            return entries.ToArray();
        }

        public void CreateDirectory(string path)
        {
            AddDirectory(path);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            var source = NormalizePath(sourcePath);
            var destination = NormalizePath(destinationPath);

            if (!_files.Remove(source))
            {
                throw new FileNotFoundException($"File not found: {sourcePath}");
            }

            AddFile(destination);
        }

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            var source = NormalizePath(sourcePath);
            var destination = NormalizePath(destinationPath);

            if (!_directories.Contains(source))
            {
                throw new DirectoryNotFoundException($"Directory not found: {sourcePath}");
            }

            var directoriesToMove = _directories
                .Where(entry => IsNestedPath(entry, source) || _comparer.Equals(entry, source))
                .OrderBy(entry => entry.Length)
                .ToList();

            var filesToMove = _files
                .Where(entry => IsNestedPath(entry, source))
                .ToList();

            foreach (var entry in directoriesToMove)
            {
                _directories.Remove(entry);
                var relative = Path.GetRelativePath(source, entry);
                var target = Path.Combine(destination, relative);
                AddDirectory(target);
            }

            foreach (var entry in filesToMove)
            {
                _files.Remove(entry);
                var relative = Path.GetRelativePath(source, entry);
                var target = Path.Combine(destination, relative);
                AddFile(target);
            }
        }

        public void DeleteFile(string path)
        {
            var normalized = NormalizePath(path);
            _files.Remove(normalized);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            var normalized = NormalizePath(path);
            if (recursive)
            {
                var directoriesToRemove = _directories
                    .Where(entry => IsNestedPath(entry, normalized) || _comparer.Equals(entry, normalized))
                    .ToList();

                var filesToRemove = _files
                    .Where(entry => IsNestedPath(entry, normalized))
                    .ToList();

                foreach (var entry in directoriesToRemove)
                {
                    _directories.Remove(entry);
                }

                foreach (var entry in filesToRemove)
                {
                    _files.Remove(entry);
                }

                return;
            }

            var hasChildren = _directories.Any(entry =>
                    !_comparer.Equals(entry, normalized)
                    && IsNestedPath(entry, normalized))
                || _files.Any(entry => IsNestedPath(entry, normalized));

            if (hasChildren)
            {
                throw new IOException($"Directory not empty: {path}");
            }

            _directories.Remove(normalized);
        }

        public void AddDirectory(string path)
        {
            var normalized = NormalizePath(path);
            _directories.Add(normalized);
            EnsureParents(normalized);
        }

        public void AddFile(string path)
        {
            var normalized = NormalizePath(path);
            _files.Add(normalized);

            var parent = Path.GetDirectoryName(normalized);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                _directories.Add(parent);
                EnsureParents(parent);
            }
        }

        private void EnsureParents(string path)
        {
            var current = path;
            while (true)
            {
                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrWhiteSpace(parent))
                {
                    return;
                }

                if (!_directories.Add(parent))
                {
                    return;
                }

                current = parent;
            }
        }

        private bool IsDirectChild(string root, string entry)
        {
            if (_comparer.Equals(root, entry))
            {
                return false;
            }

            var relative = Path.GetRelativePath(root, entry);
            if (relative.StartsWith("..", StringComparison.Ordinal))
            {
                return false;
            }

            return !relative.Contains(Path.DirectorySeparatorChar)
                && !relative.Contains(Path.AltDirectorySeparatorChar);
        }

        private bool IsNestedPath(string child, string parent)
        {
            if (child.Length <= parent.Length)
            {
                return false;
            }

            if (!child.StartsWith(parent, GetPathComparison()))
            {
                return false;
            }

            var boundary = child[parent.Length];
            return boundary == Path.DirectorySeparatorChar || boundary == Path.AltDirectorySeparatorChar;
        }

        private string NormalizePath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);

            if (!string.IsNullOrWhiteSpace(root) && fullPath.Length > root.Length)
            {
                fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return fullPath;
        }

        private static StringComparison GetPathComparison() =>
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}

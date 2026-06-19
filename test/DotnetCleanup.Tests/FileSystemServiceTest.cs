using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Tests.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class FileSystemServiceTest
{
    private static readonly string RootPath = InMemoryFileSystem.DefaultRootPath;
    private static readonly string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void GetPaths_EnumeratesMatchingFilesAndDirectories()
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var logFilePath = Root("projectA", "artifacts", "build.log");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                binPath,
                Root("projectA", "artifacts")
            ],
            files:
            [
                logFilePath
            ]);
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/bin", "**/*.log"]);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).OrderBy(path => path.Value, StringComparer.OrdinalIgnoreCase).ToArray();

        // Assert
        Assert.Equal(
            new[] { logFilePath, binPath }.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            paths.Select(path => path.Value).ToArray());
        Assert.Contains(paths, path => path.IsFile && path.Value == logFilePath);
        Assert.Contains(paths, path => !path.IsFile && path.Value == binPath);
    }

    [Fact]
    public void GetPaths_WhenFileEnumerationThrowsMidTraversal_ReturnsPartialMatchesAndFailure()
    {
        // Arrange
        var firstLogPath = Root("a-first.log");
        var secondLogPath = Root("b-second.log");
        var fileSystem = new ThrowsDuringFileEnumerationFileSystem(
            [RootPath, TempPath],
            [firstLogPath, secondLogPath],
            RootPath,
            new IOException("mid-traversal file failure"));
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/*.log"]);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).ToArray();

        // Assert
        var listedPath = Assert.Single(paths, path => path.Exception == null);
        var failedPath = Assert.Single(paths, path => path.Exception != null);

        Assert.Equal(firstLogPath, listedPath.Value);
        Assert.True(listedPath.IsFile);
        Assert.Equal(RootPath, failedPath.Value);
        Assert.Equal(PathFailureStage.List, failedPath.FailedOn);
        Assert.IsType<IOException>(failedPath.Exception);
    }

    [Fact]
    public void GetPaths_WhenDirectoryEnumerationThrowsMidTraversal_ReturnsPartialMatchesAndFailure()
    {
        // Arrange
        var projectABinPath = Root("projectA", "bin");
        var projectBBinPath = Root("projectB", "bin");
        var fileSystem = new ThrowsDuringDirectoryEnumerationFileSystem(
            [
                RootPath,
                TempPath,
                Root("projectA"),
                projectABinPath,
                Root("projectB"),
                projectBBinPath
            ],
            RootPath,
            new IOException("mid-traversal directory failure"));
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/bin"]);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).ToArray();

        // Assert
        var listedPath = Assert.Single(paths, path => path.Exception == null);
        var failedPath = Assert.Single(paths, path => path.Exception != null);

        Assert.Equal(projectABinPath, listedPath.Value);
        Assert.False(listedPath.IsFile);
        Assert.Equal(RootPath, failedPath.Value);
        Assert.Equal(PathFailureStage.List, failedPath.FailedOn);
        Assert.IsType<IOException>(failedPath.Exception);
    }

    [Fact]
    public void GetPaths_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var projectPath = Root("projectA");
        var binPath = Root("projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                projectPath,
                binPath
            ]);
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act / Assert
        Assert.Throws<OperationCanceledException>(() => service.GetPaths(settings, cancellationTokenSource.Token).ToArray());
    }

    [Fact]
    public void MovePath_BuildsTheExpectedTargetPathForFiles()
    {
        // Arrange
        var sourceFilePath = Root("projectA", "artifacts", "build.log");
        var tempRunPath = TempRunPath("test");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "artifacts"),
                tempRunPath
            ],
            files:
            [
                sourceFilePath
            ]);
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/*.log"]);
        var path = new PathInfo(sourceFilePath, isFile: true);

        // Act
        var movedPath = service.MovePath(tempRunPath, path, settings);
        var expectedMovePath = TestPath.Combine(tempRunPath, "projectA", "artifacts", "build.log");

        // Assert
        Assert.Same(path, movedPath);
        Assert.Equal(expectedMovePath, movedPath.MovePath);
        Assert.Contains(expectedMovePath, fileSystem.Files, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(sourceFilePath, fileSystem.Files, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeletePath_DeletesTheStagedPathWhenMovePathExists()
    {
        // Arrange
        var originalPath = Root("projectA", "bin");
        var stagedPath = TempRunPath("test", "projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                originalPath,
                TempRunPath("test"),
                TempRunPath("test", "projectA"),
                stagedPath
            ]);
        var service = new FileSystemService(fileSystem);
        var path = new PathInfo(originalPath, isFile: false);
        path.SetMovePath(stagedPath);

        // Act
        var deletedPath = service.DeletePath(path);

        // Assert
        Assert.Same(path, deletedPath);
        Assert.DoesNotContain(stagedPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(originalPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureTempDirectory_CreatesUniqueDirectoriesUnderTheConfiguredTempPath()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var firstPath = service.EnsureTempDirectory(settings);
        var secondPath = service.EnsureTempDirectory(settings);

        // Assert
        Assert.NotEqual(firstPath, secondPath);
        Assert.StartsWith(GetTempRunPrefix(settings), firstPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(GetTempRunPrefix(settings), secondPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(firstPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(secondPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void MovePath_WhenDirectoryDisappears_ReportsPerPathFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var binPath = Root("projectA", "bin");
        var tempRunPath = TempRunPath("test");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                binPath,
                tempRunPath
            ]);
        fileSystem.MoveDirectoryExceptions.Add(binPath, new DirectoryNotFoundException("directory vanished"));

        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem);
        var path = new PathInfo(binPath, isFile: false);

        // Act
        var result = service.MovePath(tempRunPath, path, settings);

        // Assert
        Assert.Same(path, result);
        Assert.Equal(PathFailureStage.Move, result.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(result.Exception);
    }

    [Fact]
    public void MovePath_WhenFileDisappears_ReportsPerPathFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var filePath = Root("projectA", "build.log");
        var tempRunPath = TempRunPath("test");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                tempRunPath
            ],
            files: [filePath]);
        fileSystem.MoveFileExceptions.Add(filePath, new FileNotFoundException("file vanished"));

        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/*.log"]);
        var path = new PathInfo(filePath, isFile: true);

        // Act
        var result = service.MovePath(tempRunPath, path, settings);

        // Assert
        Assert.Same(path, result);
        Assert.Equal(PathFailureStage.Move, result.FailedOn);
        Assert.IsType<FileNotFoundException>(result.Exception);
    }

    [Fact]
    public void DeletePath_WhenStagedDirectoryDisappears_ReportsPerPathFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var originalPath = Root("projectA", "bin");
        var stagedPath = TempRunPath("test", "projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                originalPath,
                TempRunPath("test"),
                TempRunPath("test", "projectA"),
                stagedPath
            ]);
        fileSystem.DeleteDirectoryExceptions.Add(stagedPath, new DirectoryNotFoundException("staged directory vanished"));

        var service = new FileSystemService(fileSystem);
        var path = new PathInfo(originalPath, isFile: false);
        path.SetMovePath(stagedPath);

        // Act
        var result = service.DeletePath(path);

        // Assert
        Assert.Same(path, result);
        Assert.Equal(PathFailureStage.Delete, result.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(result.Exception);
    }

    [Fact]
    public void GetPaths_WhenFileEnumerationFailsMidTraversal_ReportsPartialResultsAndFailure()
    {
        // Arrange (#24 - mid-traversal enumeration exception)
        var projectAPath = Root("projectA");
        var projectABinPath = Root("projectA", "bin");
        var projectBPath = Root("projectB");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                projectAPath,
                projectABinPath,
                projectBPath
            ]);

        // projectA enumerates fine, but projectB fails during file enumeration
        fileSystem.ListFileExceptions.Add(projectBPath, new IOException("access denied during file enumeration"));

        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).ToArray();

        // Assert
        Assert.Contains(paths, p => p.Value == projectABinPath && p.Exception == null);
        Assert.Contains(paths, p => p.Value == projectBPath && p.Exception != null && p.FailedOn == PathFailureStage.List);
    }

    [Fact]
    public void GetPaths_WhenDirectoryEnumerationFailsMidTraversal_ReportsPartialResultsAndFailure()
    {
        // Arrange (#24 - mid-traversal enumeration exception)
        var projectAPath = Root("projectA");
        var projectABinPath = Root("projectA", "bin");
        var projectBPath = Root("projectB");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                projectAPath,
                projectABinPath,
                projectBPath
            ]);

        // root enumerates projectA, then fails while yielding projectB
        fileSystem.YieldDirectoryExceptions.Add(projectBPath, new IOException("access denied during directory enumeration"));

        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).ToArray();

        // Assert
        Assert.Contains(paths, p => p.Value == projectABinPath && p.Exception == null);
        Assert.Contains(paths, p => p.Value == RootPath && p.Exception != null && p.FailedOn == PathFailureStage.List);
    }

    [Fact]
    public void GetPaths_WhenDirectoryMatches_DoesNotRecurseIntoMatchedDirectory()
    {
        // Arrange
        var projectPath = Root("projectA");
        var binPath = Root("projectA", "bin");
        var nestedBinPath = Root("projectA", "bin", "nested", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                projectPath,
                binPath,
                Root("projectA", "bin", "nested"),
                nestedBinPath
            ]);
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/bin"]);

        // Act
        var paths = service.GetPaths(settings, CancellationToken.None).ToArray();

        // Assert
        var path = Assert.Single(paths);
        Assert.Equal(binPath, path.Value);
        Assert.DoesNotContain(paths, x => x.Value == nestedBinPath);
    }

    private static CleanupSettings CreateSettings(IFileSystem fileSystem, string[]? include = null)
    {
        return new CleanupSettings(fileSystem)
        {
            Path = RootPath,
            TempPath = TempPath,
            Include = include ?? ["**/bin"],
            SkipConfirm = true
        };
    }

    private static string Root(params string[] segments) => TestPath.Root(segments);

    private static string TempRunPath(string suffix, params string[] segments)
    {
        return CleanupTempPath.CreatePath(TempPath, $"{CleanupTempPath.DirectoryNamePrefix}-{suffix}", segments);
    }

    private static string GetTempRunPrefix(CleanupSettings settings) => CleanupTempPath.GetRunDirectoryPrefix(TempPath, settings.StartedAt);

    private sealed class ThrowsDuringFileEnumerationFileSystem(string[] directories, string[] files, string failingPath, Exception exception) : InMemoryFileSystem(directories, files)
    {
        private readonly Exception _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        private readonly string _failingPath = failingPath ?? throw new ArgumentNullException(nameof(failingPath));
        private bool _hasThrown;

        public override IEnumerable<string> EnumerateFiles(string path)
        {
            if (_hasThrown || !string.Equals(path, _failingPath, StringComparison.OrdinalIgnoreCase))
            {
                return base.EnumerateFiles(path);
            }

            return EnumerateFilesWithFailure(path);
        }

        private IEnumerable<string> EnumerateFilesWithFailure(string path)
        {
            var files = base.EnumerateFiles(path)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length > 0)
            {
                yield return files[0];
            }

            _hasThrown = true;
            throw _exception;
        }
    }

    private sealed class ThrowsDuringDirectoryEnumerationFileSystem(string[] directories, string failingPath, Exception exception) : InMemoryFileSystem(directories)
    {
        private readonly Exception _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        private readonly string _failingPath = failingPath ?? throw new ArgumentNullException(nameof(failingPath));
        private bool _hasThrown;

        public override IEnumerable<string> EnumerateDirectories(string path)
        {
            if (_hasThrown || !string.Equals(path, _failingPath, StringComparison.OrdinalIgnoreCase))
            {
                return base.EnumerateDirectories(path);
            }

            return EnumerateDirectoriesWithFailure(path);
        }

        private IEnumerable<string> EnumerateDirectoriesWithFailure(string path)
        {
            var directories = base.EnumerateDirectories(path)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (directories.Length > 0)
            {
                yield return directories[0];
            }

            _hasThrown = true;
            throw _exception;
        }
    }

}

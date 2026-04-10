using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupServiceTests
{
    private static readonly string RootPath = InMemoryFileSystem.DefaultRootPath;
    private static readonly string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void Cleanup_GlobIncludePatterns_ListsOnlyMatchingPaths()
    {
        // Arrange
        var projectAObjPath = Root("projectA", "obj");
        var projectABinPath = Root("projectA", "bin");
        var projectBObjPath = Root("projectB", "obj");

        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                projectABinPath,
                projectAObjPath,
                Root("projectB"),
                projectBObjPath
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(
            fileSystem,
            noop: true,
            include: ["**/obj"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        Assert.Equal(
            [projectAObjPath, projectBObjPath],
            result.ListStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_GlobExcludePatterns_ExcludesMatchingPaths()
    {
        // Arrange
        var projectABinPath = Root("projectA", "bin");
        var projectBBinPath = Root("projectB", "bin");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                projectABinPath,
                Root("projectB"),
                projectBBinPath
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(
            fileSystem,
            noop: true,
            include: ["**/bin"],
            exclude: ["projectA/**"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        Assert.Equal(
            [projectBBinPath],
            result.ListStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_ExcludePatterns_TakePrecedenceOverIncludePatterns()
    {
        // Arrange
        var projectABinPath = Root("projectA", "bin");
        var projectBBinPath = Root("projectB", "bin");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                projectABinPath,
                Root("projectB"),
                projectBBinPath
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(
            fileSystem,
            noop: true,
            include: ["**/bin", "projectA/bin"],
            exclude: ["projectA/bin"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        Assert.Equal(
            [projectBBinPath],
            result.ListStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_UsesSinglePathInfoInstanceAcrossListMoveAndDelete()
    {
        // Arrange
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("src"),
                Root("src", "bin")
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);
        var tempRunPath = GetTempRunPath(fileSystem, settings);
        var expectedMovedPath = CombinePath(tempRunPath, "src", "bin");

        // Assert
        var listPath = Assert.Single(result.ListStep.Successes);
        var movePath = Assert.Single(result.MoveStep.Successes);
        var deletePath = Assert.Single(result.DeleteStep.Successes);

        Assert.Same(listPath, movePath);
        Assert.Same(listPath, deletePath);
        Assert.Null(listPath.Exception);
        Assert.Null(listPath.FailedOn);
        Assert.Equal(expectedMovedPath, listPath.MovePath);
    }

    [Fact]
    public void Cleanup_MarksMoveFailuresOnPathInfoAndAddsToMoveFailed()
    {
        // Arrange
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        fileSystem.MoveDirectoryExceptions.Add(binPath, new IOException("move failed"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.ListStep.Successes);
        var failedMovePath = Assert.Single(result.MoveStep.Failed);

        Assert.Same(listPath, failedMovePath);
        Assert.Equal(PathFailureStage.Move, failedMovePath.FailedOn);
        Assert.IsType<IOException>(failedMovePath.Exception);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
    }

    [Fact]
    public void Cleanup_MarksDeleteFailuresOnPathInfoAndAddsToDeleteFailed()
    {
        // Arrange
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        service.OnMovePath += path => fileSystem.DeleteDirectoryExceptions.TryAdd(path.MovePath, new IOException("delete failed"));

        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var movedPath = Assert.Single(result.MoveStep.Successes);
        var failedDeletePath = Assert.Single(result.DeleteStep.Failed);

        Assert.Same(movedPath, failedDeletePath);
        Assert.Equal(PathFailureStage.Delete, failedDeletePath.FailedOn);
        Assert.IsType<IOException>(failedDeletePath.Exception);
        Assert.Empty(result.DeleteStep.Successes);
    }

    [Fact]
    public void Cleanup_DeletesOriginalPathsWhenSkipMoveIsEnabled()
    {
        // Arrange
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        var movePathEventCount = 0;
        var deletePathEventCount = 0;

        service.OnMovePath += (_) => movePathEventCount++;
        service.OnDeletePath += (_) => deletePathEventCount++;

        var settings = CreateSettings(fileSystem, skipMove: true);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.ListStep.Successes);
        var movePath = Assert.Single(result.MoveStep.Successes);
        var deletePath = Assert.Single(result.DeleteStep.Successes);

        Assert.Same(listPath, movePath);
        Assert.Same(listPath, deletePath);
        Assert.True(string.IsNullOrWhiteSpace(movePath.MovePath));
        Assert.DoesNotContain(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1, movePathEventCount);
        Assert.Equal(1, deletePathEventCount);
    }

    [Fact]
    public void Cleanup_SkipsMoveAndDeleteWhenNoopIsEnabled()
    {
        // Arrange
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        var movePathEventCount = 0;
        var deletePathEventCount = 0;

        service.OnMovePath += (_) => movePathEventCount++;
        service.OnDeletePath += (_) => deletePathEventCount++;

        var settings = CreateSettings(fileSystem, noop: true);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.ListStep.Successes);

        Assert.Equal(binPath, listPath.Value);
        Assert.Empty(result.MoveStep.Successes);
        Assert.Empty(result.MoveStep.Failed);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
        Assert.Contains(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, movePathEventCount);
        Assert.Equal(0, deletePathEventCount);
    }

    [Fact]
    public void Cleanup_MovesPathsAndSkipsDeleteWhenSkipDeleteIsEnabled()
    {
        // Arrange
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        var movePathEventCount = 0;
        var deletePathEventCount = 0;

        service.OnMovePath += (_) => movePathEventCount++;
        service.OnDeletePath += (_) => deletePathEventCount++;

        var settings = CreateSettings(fileSystem, skipDelete: true);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);
        var tempRunPath = GetTempRunPath(fileSystem, settings);
        var movedBinPath = CombinePath(tempRunPath, "bin");

        // Assert
        var movedPath = Assert.Single(result.MoveStep.Successes);

        Assert.Equal(binPath, movedPath.Value);
        Assert.Equal(movedBinPath, movedPath.MovePath);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
        Assert.DoesNotContain(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(movedBinPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1, movePathEventCount);
        Assert.Equal(0, deletePathEventCount);
    }

    [Fact]
    public void Cleanup_MarksListFailuresOnPathInfoAndAddsToListFailed()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        fileSystem.ListFileExceptions.Add(RootPath, new IOException("list failed"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var failedListPath = Assert.Single(result.ListStep.Failed);

        Assert.Equal(PathFailureStage.List, failedListPath.FailedOn);
        Assert.IsType<IOException>(failedListPath.Exception);
        Assert.Empty(result.MoveStep.Successes);
        Assert.Empty(result.MoveStep.Failed);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
    }

    [Fact]
    public void Cleanup_TracksDifferentPathFailuresAcrossListMoveAndDeleteStages()
    {
        // Arrange
        var projectABinPath = Root("projectA", "bin");
        var projectBBinPath = Root("projectB", "bin");
        var projectCBinPath = Root("projectC", "bin");
        var brokenProjectPath = Root("brokenProject");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                projectABinPath,
                Root("projectB"),
                projectBBinPath,
                Root("projectC"),
                projectCBinPath,
                brokenProjectPath,
                CombinePath(brokenProjectPath, "bin")
            ]);

        fileSystem.ListDirectoryExceptions.Add(CombinePath(brokenProjectPath, "bin"), new IOException("list failed for path"));
        fileSystem.MoveDirectoryExceptions.Add(projectBBinPath, new IOException("move failed for path"));

        var service = new CleanupService(fileSystem);
        service.OnMovePath += path =>
        {
            if (string.Equals(path.Value, projectCBinPath, StringComparison.OrdinalIgnoreCase))
            {
                fileSystem.DeleteDirectoryExceptions.TryAdd(path.MovePath, new IOException("delete failed for path"));
            }
        };

        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listFailedPath = Assert.Single(result.ListStep.Failed);
        Assert.Equal(brokenProjectPath, listFailedPath.Value);
        Assert.Equal(PathFailureStage.List, listFailedPath.FailedOn);

        var moveFailedPath = Assert.Single(result.MoveStep.Failed);
        Assert.Equal(projectBBinPath, moveFailedPath.Value);
        Assert.Equal(PathFailureStage.Move, moveFailedPath.FailedOn);
        Assert.True(string.IsNullOrWhiteSpace(moveFailedPath.MovePath));

        var moveSucceededProjectA = Assert.Single(result.MoveStep.Successes, x => x.Value == projectABinPath);
        Assert.StartsWith(GetTempRunPrefix(settings), moveSucceededProjectA.MovePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("projectA/bin", moveSucceededProjectA.MovePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

        var deleteFailedPath = Assert.Single(result.DeleteStep.Failed);
        Assert.Equal(projectCBinPath, deleteFailedPath.Value);
        Assert.Equal(PathFailureStage.Delete, deleteFailedPath.FailedOn);
        Assert.StartsWith(GetTempRunPrefix(settings), deleteFailedPath.MovePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("projectC/bin", deleteFailedPath.MovePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

        var deleteSucceededPath = Assert.Single(result.DeleteStep.Successes);
        Assert.Equal(projectABinPath, deleteSucceededPath.Value);

        Assert.DoesNotContain(result.DeleteStep.Successes, x => x.Value == projectBBinPath);
        Assert.DoesNotContain(result.DeleteStep.Failed, x => x.Value == projectBBinPath);
    }

    [Fact]
    public void Cleanup_FileMatches_MovesAndDeletesFiles()
    {
        // Arrange
        var logFilePath = Root("projectA", "artifacts", "build.log");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                Root("projectA", "artifacts")
            ],
            files:
            [
                logFilePath
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/*.log"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);
        var tempRunPath = GetTempRunPath(fileSystem, settings);
        var movedLogFilePath = CombinePath(tempRunPath, "projectA", "artifacts", "build.log");

        // Assert
        var listedPath = Assert.Single(result.ListStep.Successes);
        var movedPath = Assert.Single(result.MoveStep.Successes);
        var deletedPath = Assert.Single(result.DeleteStep.Successes);

        Assert.True(listedPath.IsFile);
        Assert.Same(listedPath, movedPath);
        Assert.Same(listedPath, deletedPath);
        Assert.Equal(movedLogFilePath, listedPath.MovePath);
        Assert.DoesNotContain(logFilePath, fileSystem.Files, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(movedLogFilePath, fileSystem.Files, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cleanup_MixedFileAndDirectoryMatches_TracksBothPathKinds()
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var logFilePath = Root("projectA", "artifacts", "build.log");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                binPath,
                Root("projectA", "artifacts")
            ],
            files:
            [
                logFilePath
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem, skipDelete: true, include: ["**/bin", "**/*.log"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.ListStep.Successes.Count);
        Assert.Equal(2, result.MoveStep.Successes.Count);
        Assert.Contains(result.ListStep.Successes, path => !path.IsFile && path.Value == binPath);
        Assert.Contains(result.ListStep.Successes, path => path.IsFile && path.Value == logFilePath);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
    }

    [Fact]
    public void Cleanup_PathDisappearsAfterListing_MarksMoveFailureInsteadOfThrowing()
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                binPath
            ]);
        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(
            () =>
            {
                fileSystem.DeleteDirectory(binPath);
                return true;
            },
            settings,
            CancellationToken.None);

        // Assert
        var listedPath = Assert.Single(result.ListStep.Successes);
        var failedMovePath = Assert.Single(result.MoveStep.Failed);

        Assert.Same(listedPath, failedMovePath);
        Assert.Equal(PathFailureStage.Move, failedMovePath.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(failedMovePath.Exception);
        Assert.Empty(result.DeleteStep.Successes);
        Assert.Empty(result.DeleteStep.Failed);
    }

    [Fact]
    public void Cleanup_StagedPathDisappearsBeforeDelete_MarksDeleteFailureInsteadOfThrowing()
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                binPath
            ]);
        var service = new CleanupService(fileSystem);
        service.OnMovePath += path => fileSystem.DeleteDirectory(path.MovePath);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var failedDeletePath = Assert.Single(result.DeleteStep.Failed);

        Assert.Equal(binPath, failedDeletePath.Value);
        Assert.Equal(PathFailureStage.Delete, failedDeletePath.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(failedDeletePath.Exception);
        Assert.Empty(result.DeleteStep.Successes);
    }

    [Fact]
    public void Cleanup_WhenDirectoryEnumerationThrowsMidTraversal_CollectsPartialResultsAndFailure()
    {
        // Arrange
        var projectABinPath = Root("projectA", "bin");
        var projectBBinPath = Root("projectB", "bin");
        var innerFileSystem = CreateFileSystem(
            directories:
            [
                Root("projectA"),
                projectABinPath,
                Root("projectB"),
                projectBBinPath
            ]);
        var fileSystem = new ThrowsDuringDirectoryEnumerationFileSystem(innerFileSystem, RootPath, new IOException("mid-traversal directory failure"));
        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/bin"]);

        // Act
        var result = service.Cleanup(() => false, settings, CancellationToken.None);

        // Assert
        var listedPath = Assert.Single(result.ListStep.Successes);
        var failedPath = Assert.Single(result.ListStep.Failed);

        Assert.Equal(projectABinPath, listedPath.Value);
        Assert.Equal(RootPath, failedPath.Value);
        Assert.Equal(PathFailureStage.List, failedPath.FailedOn);
        Assert.IsType<IOException>(failedPath.Exception);
        Assert.Empty(result.MoveStep.Successes);
        Assert.Empty(result.MoveStep.Failed);
    }

    [Fact]
    public void Cleanup_DirectoryDisappearsBeforeMove_ReportsPerPathMoveFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        // Simulate the directory vanishing between list and move
        fileSystem.MoveDirectoryExceptions.Add(binPath, new DirectoryNotFoundException("directory vanished"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.ListStep.Successes);
        var failedMovePath = Assert.Single(result.MoveStep.Failed);

        Assert.Same(listPath, failedMovePath);
        Assert.Equal(PathFailureStage.Move, failedMovePath.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(failedMovePath.Exception);
    }

    [Fact]
    public void Cleanup_FileDisappearsBeforeMove_ReportsPerPathMoveFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var filePath = Root("project", "artifacts", "build.log");
        var fileSystem = CreateFileSystem(
            directories:
            [
                Root("project"),
                Root("project", "artifacts")
            ],
            files: [filePath]);

        fileSystem.MoveFileExceptions.Add(filePath, new FileNotFoundException("file vanished"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem, include: ["**/*.log"]);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var failedMovePath = Assert.Single(result.MoveStep.Failed);

        Assert.Equal(PathFailureStage.Move, failedMovePath.FailedOn);
        Assert.IsType<FileNotFoundException>(failedMovePath.Exception);
    }

    [Fact]
    public void Cleanup_DirectoryDisappearsBeforeDelete_ReportsPerPathDeleteFailure()
    {
        // Arrange (#23 - disappearing-path regression)
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        service.OnMovePath += path =>
        {
            // After move, the staged path will exist.
            // Simulate it disappearing before delete.
            fileSystem.DeleteDirectoryExceptions.TryAdd(
                path.MovePath,
                new DirectoryNotFoundException("staged directory vanished"));
        };

        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var failedDeletePath = Assert.Single(result.DeleteStep.Failed);

        Assert.Equal(PathFailureStage.Delete, failedDeletePath.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(failedDeletePath.Exception);
    }

    [Fact]
    public void Cleanup_DirectoryDisappearsBeforeDeleteWithSkipMove_ReportsPerPathDeleteFailure()
    {
        // Arrange (#23 - disappearing-path regression, skip-move variant)
        var binPath = Root("bin");
        var fileSystem = CreateFileSystem(directories: [binPath]);

        // When skip-move is used, the original path is deleted directly
        fileSystem.DeleteDirectoryExceptions.Add(binPath, new DirectoryNotFoundException("directory vanished"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem, skipMove: true);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var failedDeletePath = Assert.Single(result.DeleteStep.Failed);

        Assert.Equal(PathFailureStage.Delete, failedDeletePath.FailedOn);
        Assert.IsType<DirectoryNotFoundException>(failedDeletePath.Exception);
    }

    private static InMemoryFileSystem CreateFileSystem(string[]? directories = null, string[]? files = null)
    {
        if (directories == null && files == null)
        {
            return new InMemoryFileSystem();
        }

        string[] allDirectories = directories == null
            ? [RootPath, TempPath]
            : [RootPath, TempPath, .. directories];

        return new InMemoryFileSystem(allDirectories, files);
    }

    private static CleanupSettings CreateSettings(
        IFileSystem fileSystem,
        bool skipMove = false,
        bool skipDelete = false,
        bool noop = false,
        string[]? include = null,
        string[]? exclude = null)
    {
        return new CleanupSettings(fileSystem)
        {
            Path = RootPath,
            TempPath = TempPath,
            Include = include ?? ["**/bin"],
            Exclude = exclude ?? [],
            SkipConfirm = true,
            Noop = noop,
            SkipMove = skipMove,
            SkipDelete = skipDelete
        };
    }

    private static string Root(params string[] segments) => TestPath.Root(segments);

    private static string CombinePath(string path, params string[] segments) => TestPath.Combine(path, segments);

    private static string GetTempRunPrefix(CleanupSettings settings) => CleanupTempPath.GetRunDirectoryPrefix(TempPath, settings.StartedAt);

    private static string GetTempRunPath(InMemoryFileSystem fileSystem, CleanupSettings settings)
    {
        var expectedPrefix = GetTempRunPrefix(settings);

        return Assert.Single(
            fileSystem.Directories,
            path => path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) &&
                Path.GetRelativePath(TempPath, path).IndexOfAny(['\\', '/']) < 0);
    }

    private sealed class ThrowsDuringDirectoryEnumerationFileSystem(InMemoryFileSystem innerFileSystem, string failingPath, Exception exception) : IFileSystem
    {
        private readonly InMemoryFileSystem _innerFileSystem = innerFileSystem ?? throw new ArgumentNullException(nameof(innerFileSystem));
        private readonly Exception _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        private readonly string _failingPath = failingPath ?? throw new ArgumentNullException(nameof(failingPath));
        private bool _hasThrown;

        public void CreateDirectory(string path) => _innerFileSystem.CreateDirectory(path);

        public void DeleteDirectory(string path) => _innerFileSystem.DeleteDirectory(path);

        public void DeleteFile(string path) => _innerFileSystem.DeleteFile(path);

        public bool DirectoryExists(string path) => _innerFileSystem.DirectoryExists(path);

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            if (_hasThrown || !string.Equals(path, _failingPath, StringComparison.OrdinalIgnoreCase))
            {
                return _innerFileSystem.EnumerateDirectories(path);
            }

            return EnumerateDirectoriesWithFailure(path);
        }

        public IEnumerable<string> EnumerateFiles(string path) => _innerFileSystem.EnumerateFiles(path);

        public string GetCurrentDirectory() => _innerFileSystem.GetCurrentDirectory();

        public string GetTempPath() => _innerFileSystem.GetTempPath();

        public void MoveDirectory(string sourcePath, string destinationPath) => _innerFileSystem.MoveDirectory(sourcePath, destinationPath);

        public void MoveFile(string sourcePath, string destinationPath) => _innerFileSystem.MoveFile(sourcePath, destinationPath);

        private IEnumerable<string> EnumerateDirectoriesWithFailure(string path)
        {
            var directories = _innerFileSystem.EnumerateDirectories(path)
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

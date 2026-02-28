using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotNetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupServiceTests
{
    private const string RootPath = InMemoryFileSystem.DefaultRootPath;
    private const string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void Cleanup_GlobIncludePatterns_ListsOnlyMatchingPaths()
    {
        // Arrange
        var projectAObjPath = $@"{RootPath}\projectA\obj";
        var projectABinPath = $@"{RootPath}\projectA\bin";
        var projectBObjPath = $@"{RootPath}\projectB\obj";

        var fileSystem = CreateFileSystem(
            directories:
            [
                $@"{RootPath}\projectA",
                projectABinPath,
                projectAObjPath,
                $@"{RootPath}\projectB",
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
            result.GetStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_GlobExcludePatterns_ExcludesMatchingPaths()
    {
        // Arrange
        var projectABinPath = $@"{RootPath}\projectA\bin";
        var projectBBinPath = $@"{RootPath}\projectB\bin";
        var fileSystem = CreateFileSystem(
            directories:
            [
                $@"{RootPath}\projectA",
                projectABinPath,
                $@"{RootPath}\projectB",
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
            result.GetStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_ExcludePatterns_TakePrecedenceOverIncludePatterns()
    {
        // Arrange
        var projectABinPath = $@"{RootPath}\projectA\bin";
        var projectBBinPath = $@"{RootPath}\projectB\bin";
        var fileSystem = CreateFileSystem(
            directories:
            [
                $@"{RootPath}\projectA",
                projectABinPath,
                $@"{RootPath}\projectB",
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
            result.GetStep.Successes.Select(x => x.Value).Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void Cleanup_UsesSinglePathInfoInstanceAcrossListMoveAndDelete()
    {
        // Arrange
        var fileSystem = CreateFileSystem(
            directories:
            [
                $@"{RootPath}\src",
                $@"{RootPath}\src\bin"
            ]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);
        var tempRunPath = Path.Combine(TempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
        var expectedMovedPath = Path.Combine(tempRunPath, @"src\bin");

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.GetStep.Successes);
        var movePath = Assert.Single(result.MoveStep!.Successes);
        var deletePath = Assert.Single(result.DeleteStep!.Successes);

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
        var binPath = $@"{RootPath}\bin";
        var fileSystem = CreateFileSystem(directories: [binPath]);

        fileSystem.MoveDirectoryExceptions.Add(binPath, new IOException("move failed"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.GetStep.Successes);
        var failedMovePath = Assert.Single(result.MoveStep!.Failed);

        Assert.Same(listPath, failedMovePath);
        Assert.Equal(PathFailureStage.Move, failedMovePath.FailedOn);
        Assert.IsType<IOException>(failedMovePath.Exception);
        Assert.Empty(result.DeleteStep!.Successes);
        Assert.Empty(result.DeleteStep!.Failed);
    }

    [Fact]
    public void Cleanup_MarksDeleteFailuresOnPathInfoAndAddsToDeleteFailed()
    {
        // Arrange
        var binPath = $@"{RootPath}\bin";
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);
        var tempRunPath = Path.Combine(TempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
        var movedBinPath = Path.Combine(tempRunPath, @"bin");

        fileSystem.DeleteDirectoryExceptions.Add(movedBinPath, new IOException("delete failed"));

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var movedPath = Assert.Single(result.MoveStep!.Successes);
        var failedDeletePath = Assert.Single(result.DeleteStep!.Failed);

        Assert.Same(movedPath, failedDeletePath);
        Assert.Equal(PathFailureStage.Delete, failedDeletePath.FailedOn);
        Assert.IsType<IOException>(failedDeletePath.Exception);
        Assert.Empty(result.DeleteStep!.Successes);
    }

    [Fact]
    public void Cleanup_DeletesOriginalPathsWhenSkipMoveIsEnabled()
    {
        // Arrange
        var binPath = $@"{RootPath}\bin";
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
        var listPath = Assert.Single(result.GetStep.Successes);
        var movePath = Assert.Single(result.MoveStep!.Successes);
        var deletePath = Assert.Single(result.DeleteStep!.Successes);

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
        var binPath = $@"{RootPath}\bin";
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
        var listPath = Assert.Single(result.GetStep.Successes);

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
        var binPath = $@"{RootPath}\bin";
        var fileSystem = CreateFileSystem(directories: [binPath]);

        var service = new CleanupService(fileSystem);
        var movePathEventCount = 0;
        var deletePathEventCount = 0;

        service.OnMovePath += (_) => movePathEventCount++;
        service.OnDeletePath += (_) => deletePathEventCount++;

        var settings = CreateSettings(fileSystem, skipDelete: true);
        var tempRunPath = Path.Combine(TempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
        var movedBinPath = Path.Combine(tempRunPath, @"bin");

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

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
        var failedListPath = Assert.Single(result.GetStep.Failed);

        Assert.Equal(PathFailureStage.List, failedListPath.FailedOn);
        Assert.IsType<IOException>(failedListPath.Exception);
        Assert.Empty(result.MoveStep!.Successes);
        Assert.Empty(result.MoveStep!.Failed);
        Assert.Empty(result.DeleteStep!.Successes);
        Assert.Empty(result.DeleteStep!.Failed);
    }

    [Fact]
    public void Cleanup_TracksDifferentPathFailuresAcrossListMoveAndDeleteStages()
    {
        // Arrange
        var projectABinPath = $@"{RootPath}\projectA\bin";
        var projectBBinPath = $@"{RootPath}\projectB\bin";
        var projectCBinPath = $@"{RootPath}\projectC\bin";
        var brokenProjectPath = $@"{RootPath}\brokenProject";
        var fileSystem = CreateFileSystem(
            directories:
            [
                $@"{RootPath}\projectA",
                projectABinPath,
                $@"{RootPath}\projectB",
                projectBBinPath,
                $@"{RootPath}\projectC",
                projectCBinPath,
                brokenProjectPath,
                $@"{brokenProjectPath}\bin"
            ]);

        fileSystem.ListDirectoryExceptions.Add($@"{brokenProjectPath}\bin", new IOException("list failed for path"));
        fileSystem.MoveDirectoryExceptions.Add(projectBBinPath, new IOException("move failed for path"));

        var service = new CleanupService(fileSystem);
        var settings = CreateSettings(fileSystem);
        var tempRunPath = Path.Combine(TempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
        var movedProjectAPath = Path.Combine(tempRunPath, @"projectA\bin");
        var movedProjectCPath = Path.Combine(tempRunPath, @"projectC\bin");

        fileSystem.DeleteDirectoryExceptions.Add(movedProjectCPath, new IOException("delete failed for path"));

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listFailedPath = Assert.Single(result.GetStep.Failed);
        Assert.Equal(brokenProjectPath, listFailedPath.Value);
        Assert.Equal(PathFailureStage.List, listFailedPath.FailedOn);

        var moveFailedPath = Assert.Single(result.MoveStep!.Failed);
        Assert.Equal(projectBBinPath, moveFailedPath.Value);
        Assert.Equal(PathFailureStage.Move, moveFailedPath.FailedOn);
        Assert.True(string.IsNullOrWhiteSpace(moveFailedPath.MovePath));

        var moveSucceededProjectA = Assert.Single(result.MoveStep.Successes, x => x.Value == projectABinPath);
        Assert.Equal(movedProjectAPath, moveSucceededProjectA.MovePath);

        var deleteFailedPath = Assert.Single(result.DeleteStep!.Failed);
        Assert.Equal(projectCBinPath, deleteFailedPath.Value);
        Assert.Equal(PathFailureStage.Delete, deleteFailedPath.FailedOn);
        Assert.Equal(movedProjectCPath, deleteFailedPath.MovePath);

        var deleteSucceededPath = Assert.Single(result.DeleteStep.Successes);
        Assert.Equal(projectABinPath, deleteSucceededPath.Value);

        Assert.DoesNotContain(result.DeleteStep.Successes, x => x.Value == projectBBinPath);
        Assert.DoesNotContain(result.DeleteStep.Failed, x => x.Value == projectBBinPath);
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
}

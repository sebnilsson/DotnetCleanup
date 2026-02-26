using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotNetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupServiceTests
{
    [Fact]
    public void Cleanup_UsesSinglePathInfoInstanceAcrossListMoveAndDelete()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add($@"{rootPath}\src");
        fileSystem.Directories.Add($@"{rootPath}\src\bin");

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);
        var tempRunPath = Path.Combine(tempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
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
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.MoveDirectoryExceptions.Add(binPath, new IOException("move failed"));

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

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
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);
        var tempRunPath = Path.Combine(tempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
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
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath, skipMove: true);

        // Act
        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        // Assert
        var listPath = Assert.Single(result.GetStep.Successes);
        var movePath = Assert.Single(result.MoveStep!.Successes);
        var deletePath = Assert.Single(result.DeleteStep!.Successes);

        Assert.Same(listPath, movePath);
        Assert.Same(listPath, deletePath);
        Assert.DoesNotContain(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cleanup_SkipsMoveAndDeleteWhenNoopIsEnabled()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath, noop: true);

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
    }

    [Fact]
    public void Cleanup_MovesPathsAndSkipsDeleteWhenSkipDeleteIsEnabled()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath, skipDelete: true);
        var tempRunPath = Path.Combine(tempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
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
    }

    [Fact]
    public void Cleanup_MarksListFailuresOnPathInfoAndAddsToListFailed()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.ListFileExceptions.Add(rootPath, new IOException("list failed"));

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

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
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var projectABinPath = $@"{rootPath}\projectA\bin";
        var projectBBinPath = $@"{rootPath}\projectB\bin";
        var projectCBinPath = $@"{rootPath}\projectC\bin";
        var brokenProjectPath = $@"{rootPath}\brokenProject";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add($@"{rootPath}\projectA");
        fileSystem.Directories.Add(projectABinPath);
        fileSystem.Directories.Add($@"{rootPath}\projectB");
        fileSystem.Directories.Add(projectBBinPath);
        fileSystem.Directories.Add($@"{rootPath}\projectC");
        fileSystem.Directories.Add(projectCBinPath);
        fileSystem.Directories.Add(brokenProjectPath);
        fileSystem.Directories.Add($@"{brokenProjectPath}\bin");

        fileSystem.ListDirectoryExceptions.Add($@"{brokenProjectPath}\bin", new IOException("list failed for path"));
        fileSystem.MoveDirectoryExceptions.Add(projectBBinPath, new IOException("move failed for path"));

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);
        var tempRunPath = Path.Combine(tempPath, $"~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}");
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

    private static CleanupService CreateService(IFileSystem fileSystem)
    {
        var fileSystemService = new FileSystemService(fileSystem);
        return new CleanupService(fileSystemService);
    }

    private static CleanupSettings CreateSettings(IFileSystem fileSystem, string rootPath, string tempPath, bool skipMove = false, bool skipDelete = false, bool noop = false)
    {
        return new CleanupSettings(fileSystem)
        {
            Path = rootPath,
            TempPath = tempPath,
            Include = ["**/bin"],
            Exclude = [],
            SkipConfirm = true,
            Noop = noop,
            SkipMove = skipMove,
            SkipDelete = skipDelete
        };
    }
}

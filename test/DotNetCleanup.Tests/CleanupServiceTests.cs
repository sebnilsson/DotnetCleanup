using DotNetCleanup.Tests.IO;
using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupServiceTests
{
    [Fact]
    public void Cleanup_UsesSinglePathInfoInstanceAcrossListMoveAndDelete()
    {
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add($@"{rootPath}\src");
        fileSystem.Directories.Add($@"{rootPath}\src\bin");

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        var listPath = Assert.Single(result.GetStep.Successes);
        var movePath = Assert.Single(result.MoveStep!.Successes);
        var deletePath = Assert.Single(result.DeleteStep!.Successes);

        Assert.Same(listPath, movePath);
        Assert.Same(listPath, deletePath);
        Assert.Null(listPath.Exception);
        Assert.Null(listPath.FailedOn);
        Assert.StartsWith(tempPath, listPath.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cleanup_MarksMoveFailuresOnPathInfoAndAddsToMoveFailed()
    {
        var fileSystem = new InMemoryFileSystem
        {
            MoveDirectoryException = new IOException("move failed")
        };
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add($@"{rootPath}\bin");

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

        var result = service.Cleanup(() => true, settings, CancellationToken.None);

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
        var fileSystem = new InMemoryFileSystem
        {
            DeleteDirectoryException = new IOException("delete failed")
        };
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add($@"{rootPath}\bin");

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

        var result = service.Cleanup(() => true, settings, CancellationToken.None);

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
        var fileSystem = new InMemoryFileSystem();
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";
        var binPath = $@"{rootPath}\bin";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);
        fileSystem.Directories.Add(binPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath, skipMove: true);

        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        var listPath = Assert.Single(result.GetStep.Successes);
        var movePath = Assert.Single(result.MoveStep!.Successes);
        var deletePath = Assert.Single(result.DeleteStep!.Successes);

        Assert.Same(listPath, movePath);
        Assert.Same(listPath, deletePath);
        Assert.DoesNotContain(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cleanup_MarksListFailuresOnPathInfoAndAddsToListFailed()
    {
        var fileSystem = new InMemoryFileSystem
        {
            EnumerateFilesException = new IOException("list failed")
        };
        var rootPath = @"C:\repo";
        var tempPath = @"C:\temp";

        fileSystem.Directories.Add(rootPath);
        fileSystem.Directories.Add(tempPath);

        var service = CreateService(fileSystem);
        var settings = CreateSettings(fileSystem, rootPath, tempPath);

        var result = service.Cleanup(() => true, settings, CancellationToken.None);

        var failedListPath = Assert.Single(result.GetStep.Failed);

        Assert.Equal(PathFailureStage.List, failedListPath.FailedOn);
        Assert.IsType<IOException>(failedListPath.Exception);
        Assert.Empty(result.MoveStep!.Successes);
        Assert.Empty(result.MoveStep!.Failed);
        Assert.Empty(result.DeleteStep!.Successes);
        Assert.Empty(result.DeleteStep!.Failed);
    }

    private static CleanupService CreateService(IFileSystem fileSystem)
    {
        var fileSystemService = new FileSystemService(fileSystem);
        return new CleanupService(fileSystemService);
    }

    private static CleanupSettings CreateSettings(IFileSystem fileSystem, string rootPath, string tempPath, bool skipMove = false, bool skipDelete = false)
    {
        return new CleanupSettings(fileSystem)
        {
            Path = rootPath,
            TempPath = tempPath,
            Include = ["**/bin"],
            Exclude = [],
            SkipConfirm = true,
            SkipMove = skipMove,
            SkipDelete = skipDelete
        };
    }
}

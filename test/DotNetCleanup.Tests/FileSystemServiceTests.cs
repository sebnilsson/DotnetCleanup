using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotNetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class FileSystemServiceTests
{
    private const string RootPath = InMemoryFileSystem.DefaultRootPath;
    private const string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void ValidateSettings_WhenIncludeIsEmpty_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new FileSystemService(fileSystem);
        var settings = CreateSettings(fileSystem, include: []);

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => service.ValidateSettings(settings));

        // Assert
        Assert.Contains("At least one include pattern must be specified.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPaths_EnumeratesMatchingFilesAndDirectories()
    {
        // Arrange
        var binPath = $@"{RootPath}\projectA\bin";
        var logFilePath = $@"{RootPath}\projectA\artifacts\build.log";
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                $@"{RootPath}\projectA",
                binPath,
                $@"{RootPath}\projectA\artifacts"
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
    public void MovePath_BuildsTheExpectedTargetPathForFiles()
    {
        // Arrange
        var sourceFilePath = $@"{RootPath}\projectA\artifacts\build.log";
        var tempRunPath = $@"{TempPath}\~dotnetcleanup-test";
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                $@"{RootPath}\projectA",
                $@"{RootPath}\projectA\artifacts",
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
        var expectedMovePath = Path.Combine(tempRunPath, @"projectA\artifacts\build.log");

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
        var originalPath = $@"{RootPath}\projectA\bin";
        var stagedPath = $@"{TempPath}\~dotnetcleanup-test\projectA\bin";
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                $@"{RootPath}\projectA",
                originalPath,
                $@"{TempPath}\~dotnetcleanup-test",
                $@"{TempPath}\~dotnetcleanup-test\projectA",
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
        Assert.StartsWith($@"{TempPath}\~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}-", firstPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith($@"{TempPath}\~dotnetcleanup-{settings.StartedAt:yyyyMMdd-HHmmss}-", secondPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(firstPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(secondPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
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
}

using DotnetCleanup.Tests.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class InMemoryFileSystemTests
{
    [Fact]
    public void DirectoryExists_UsesPlatformPathComparison()
    {
        // Arrange
        var directoryPath = TestPath.Root("Project");
        var fileSystem = new InMemoryFileSystem(directories: [directoryPath]);

        // Act
        var exists = fileSystem.DirectoryExists(TestPath.Root("project"));

        // Assert
        Assert.Equal(OperatingSystem.IsWindows(), exists);
    }

    [Fact]
    public void MoveDirectory_UsesPlatformPathComparison()
    {
        // Arrange
        var sourcePath = TestPath.Root("Project");
        var destinationPath = TestPath.Temp("MovedProject");
        var fileSystem = new InMemoryFileSystem(directories: [sourcePath]);

        // Act
        var act = () => fileSystem.MoveDirectory(TestPath.Root("project"), destinationPath);

        // Assert
        if (OperatingSystem.IsWindows())
        {
            act();
            Assert.Contains(destinationPath, fileSystem.Directories, TestPath.PathComparer);
        }
        else
        {
            Assert.Throws<DirectoryNotFoundException>(act);
        }
    }

    [Fact]
    public void MoveDirectory_WhenDestinationExists_ThrowsIOExceptionAndLeavesSource()
    {
        // Arrange
        var sourcePath = TestPath.Root("Project");
        var destinationPath = TestPath.Temp("Project");
        var fileSystem = new InMemoryFileSystem(directories: [sourcePath, destinationPath]);

        // Act
        var act = () => fileSystem.MoveDirectory(sourcePath, destinationPath);

        // Assert
        Assert.Throws<IOException>(act);
        Assert.Contains(sourcePath, fileSystem.Directories, TestPath.PathComparer);
        Assert.Contains(destinationPath, fileSystem.Directories, TestPath.PathComparer);
    }

    [Fact]
    public void MoveFile_WhenDestinationExists_ThrowsIOExceptionAndLeavesSource()
    {
        // Arrange
        var sourcePath = TestPath.Root("Project", "build.log");
        var destinationPath = TestPath.Temp("Project", "build.log");
        var fileSystem = new InMemoryFileSystem(files: [sourcePath, destinationPath]);

        // Act
        var act = () => fileSystem.MoveFile(sourcePath, destinationPath);

        // Assert
        Assert.Throws<IOException>(act);
        Assert.Contains(sourcePath, fileSystem.Files, TestPath.PathComparer);
        Assert.Contains(destinationPath, fileSystem.Files, TestPath.PathComparer);
    }
}

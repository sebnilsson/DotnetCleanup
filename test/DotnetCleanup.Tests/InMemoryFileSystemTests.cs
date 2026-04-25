using DotnetCleanup.Testing.IO;
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
}

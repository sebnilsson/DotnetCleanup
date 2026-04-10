using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathInfoTests
{
    [Fact]
    public void Constructor_NormalizesPathValue()
    {
        // Arrange
        var rawPath = $"{TestPath.RootPath}/project/bin";

        // Act
        var path = new PathInfo(rawPath, isFile: false);

        // Assert
        Assert.Equal(PathUtility.GetNormalizedPath(rawPath), path.Value);
        Assert.Equal(path.Value, path.InitialValue);
    }

    [Fact]
    public void Constructor_SetsIsFileProperty()
    {
        // Arrange
        var filePath = TestPath.Root("file.txt");
        var directoryPath = TestPath.Root("folder");

        // Act
        var file = new PathInfo(filePath, isFile: true);
        var directory = new PathInfo(directoryPath, isFile: false);

        // Assert
        Assert.True(file.IsFile);
        Assert.False(directory.IsFile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhiteSpacePath(string? value)
    {
        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new PathInfo(value!, isFile: false));
    }

    [Fact]
    public void Raw_PreservesOriginalInput()
    {
        // Arrange
        var rawPath = $"{TestPath.RootPath}/project/bin";

        // Act
        var path = new PathInfo(rawPath, isFile: false);

        // Assert
        Assert.Equal(rawPath, path.Raw);
    }

    [Fact]
    public void Parent_ReturnsParentDirectory()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("project", "bin"), isFile: false);

        // Act / Assert
        Assert.Equal(PathUtility.GetParentPath(path.Value), path.Parent);
    }

    [Fact]
    public void SetMovePath_SetsNormalizedMovePath()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);
        var movePath = CleanupTempPath.CreatePath(TestPath.TempPath, CleanupTempPath.DirectoryNamePrefix, "bin");

        // Act
        path.SetMovePath(movePath);

        // Assert
        Assert.Equal(PathUtility.GetNormalizedPath(movePath), path.MovePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetMovePath_ThrowsOnNullOrWhiteSpaceValue(string? value)
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);

        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => path.SetMovePath(value!));
    }

    [Fact]
    public void SetFailedOnList_SetsExceptionAndFailedStage()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);
        var exception = new IOException("access denied");

        // Act
        path.SetFailedOnList(exception);

        // Assert
        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.List, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnMove_SetsExceptionAndFailedStage()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);
        var exception = new IOException("move failed");

        // Act
        path.SetFailedOnMove(exception);

        // Assert
        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.Move, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnDelete_SetsExceptionAndFailedStage()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);
        var exception = new IOException("delete failed");

        // Act
        path.SetFailedOnDelete(exception);

        // Assert
        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.Delete, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnList_ThrowsOnNullException()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => path.SetFailedOnList(null!));
    }

    [Fact]
    public void NewPathInfo_HasNoExceptionOrFailedStage()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);

        // Assert
        Assert.Null(path.Exception);
        Assert.Null(path.FailedOn);
        Assert.Equal(string.Empty, path.MovePath);
    }
}

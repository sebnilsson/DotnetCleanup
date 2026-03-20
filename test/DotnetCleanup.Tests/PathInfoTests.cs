using DotnetCleanup.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathInfoTests
{
    [Fact]
    public void Constructor_NormalizesPathValue()
    {
        var path = new PathInfo(@"c:\root/project\bin", isFile: false);

        Assert.Equal(PathUtility.GetNormalizedPath(@"c:\root/project\bin"), path.Value);
        Assert.Equal(path.Value, path.InitialValue);
    }

    [Fact]
    public void Constructor_SetsIsFileProperty()
    {
        var file = new PathInfo(@"c:\root\file.txt", isFile: true);
        var directory = new PathInfo(@"c:\root\folder", isFile: false);

        Assert.True(file.IsFile);
        Assert.False(directory.IsFile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhiteSpacePath(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PathInfo(value!, isFile: false));
    }

    [Fact]
    public void Raw_PreservesOriginalInput()
    {
        var path = new PathInfo(@"c:/root/project/bin", isFile: false);

        Assert.Equal(@"c:/root/project/bin", path.Raw);
    }

    [Fact]
    public void Parent_ReturnsParentDirectory()
    {
        var path = new PathInfo(@"c:\root\project\bin", isFile: false);

        Assert.Equal(PathUtility.GetParentPath(path.Value), path.Parent);
    }

    [Fact]
    public void SetMovePath_SetsNormalizedMovePath()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        path.SetMovePath(@"c:\temp/~dotnetcleanup/bin");

        Assert.Equal(PathUtility.GetNormalizedPath(@"c:\temp/~dotnetcleanup/bin"), path.MovePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetMovePath_ThrowsOnNullOrWhiteSpaceValue(string? value)
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        Assert.ThrowsAny<ArgumentException>(() => path.SetMovePath(value!));
    }

    [Fact]
    public void SetFailedOnList_SetsExceptionAndFailedStage()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);
        var exception = new IOException("access denied");

        path.SetFailedOnList(exception);

        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.List, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnMove_SetsExceptionAndFailedStage()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);
        var exception = new IOException("move failed");

        path.SetFailedOnMove(exception);

        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.Move, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnDelete_SetsExceptionAndFailedStage()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);
        var exception = new IOException("delete failed");

        path.SetFailedOnDelete(exception);

        Assert.Same(exception, path.Exception);
        Assert.Equal(PathFailureStage.Delete, path.FailedOn);
    }

    [Fact]
    public void SetFailedOnList_ThrowsOnNullException()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        Assert.Throws<ArgumentNullException>(() => path.SetFailedOnList(null!));
    }

    [Fact]
    public void NewPathInfo_HasNoExceptionOrFailedStage()
    {
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        Assert.Null(path.Exception);
        Assert.Null(path.FailedOn);
        Assert.Equal(string.Empty, path.MovePath);
    }
}

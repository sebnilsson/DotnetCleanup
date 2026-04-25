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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhiteSpacePath(string? value)
    {
        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new PathInfo(value!, isFile: false));
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
    public void SetFailedOnList_ThrowsOnNullException()
    {
        // Arrange
        var path = new PathInfo(TestPath.Root("bin"), isFile: false);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => path.SetFailedOnList(null!));
    }
}

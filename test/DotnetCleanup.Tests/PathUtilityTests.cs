using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathUtilityTests
{
    [Fact]
    public void GetNormalizedPath_NormalizesSlashesToPlatformSeparator()
    {
        // Arrange
        var path = "root/project/bin";

        // Act
        var result = PathUtility.GetNormalizedPath(path);

        // Assert
        Assert.Equal(TestPath.Combine("root", "project", "bin"), result);
    }

    [Fact]
    public void GetNormalizedPath_TrimsTrailingSeparator()
    {
        // Arrange
        var path = $"{TestPath.Root("project", "bin")}{Path.DirectorySeparatorChar}";

        // Act
        var result = PathUtility.GetNormalizedPath(path);

        // Assert
        Assert.DoesNotMatch(@"[\\/]+$", result!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetNormalizedPath_ReturnsNullForNullOrWhiteSpace(string? value)
    {
        // Act / Assert
        Assert.Null(PathUtility.GetNormalizedPath(value));
    }

    [Fact]
    public void GetParentPath_ReturnsParentDirectory()
    {
        // Arrange
        var path = TestPath.Root("project", "bin");

        // Act
        var result = PathUtility.GetParentPath(path);

        // Assert
        Assert.Equal(TestPath.Root("project"), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetParentPath_ReturnsNullForNullOrWhiteSpace(string? value)
    {
        // Act / Assert
        Assert.Null(PathUtility.GetParentPath(value));
    }

    [Fact]
    public void GetParentPath_HandlesRootPath()
    {
        // Arrange
        var rootPath = Path.GetPathRoot(TestPath.RootPath);

        // Act
        var result = PathUtility.GetParentPath(rootPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRelativePath_ReturnsForwardSlashRelativePath()
    {
        // Arrange
        var rootPath = TestPath.RootPath;
        var path = TestPath.Root("project", "bin");

        // Act
        var result = PathUtility.GetRelativePath(rootPath, path);

        // Assert
        Assert.Equal("project/bin", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRelativePath_ReturnsNullForNullOrWhiteSpacePath(string? value)
    {
        // Act / Assert
        Assert.Null(PathUtility.GetRelativePath(TestPath.RootPath, value));
    }
}

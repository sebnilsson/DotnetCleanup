using DotnetCleanup.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathUtilityTests
{
    [Fact]
    public void GetNormalizedPath_NormalizesSlashesToPlatformSeparator()
    {
        var result = PathUtility.GetNormalizedPath(@"c:/root/project/bin");

        Assert.Equal($@"c:{Path.DirectorySeparatorChar}root{Path.DirectorySeparatorChar}project{Path.DirectorySeparatorChar}bin", result);
    }

    [Fact]
    public void GetNormalizedPath_TrimsTrailingSeparator()
    {
        var result = PathUtility.GetNormalizedPath($@"c:\root\project\bin{Path.DirectorySeparatorChar}");

        Assert.DoesNotMatch(@"[\\/]$", result!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetNormalizedPath_ReturnsNullForNullOrWhiteSpace(string? value)
    {
        Assert.Null(PathUtility.GetNormalizedPath(value));
    }

    [Fact]
    public void GetParentPath_ReturnsParentDirectory()
    {
        var result = PathUtility.GetParentPath($@"c:{Path.DirectorySeparatorChar}root{Path.DirectorySeparatorChar}project{Path.DirectorySeparatorChar}bin");

        Assert.Equal($@"c:{Path.DirectorySeparatorChar}root{Path.DirectorySeparatorChar}project", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetParentPath_ReturnsNullForNullOrWhiteSpace(string? value)
    {
        Assert.Null(PathUtility.GetParentPath(value));
    }

    [Fact]
    public void GetParentPath_HandlesRootPath()
    {
        // Path.GetDirectoryName returns null for root paths
        var result = PathUtility.GetParentPath(@"c:\");

        Assert.Null(result);
    }

    [Fact]
    public void GetRelativePath_ReturnsForwardSlashRelativePath()
    {
        var result = PathUtility.GetRelativePath(@"c:\root", @"c:\root\project\bin");

        Assert.Equal("project/bin", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRelativePath_ReturnsNullForNullOrWhiteSpacePath(string? value)
    {
        Assert.Null(PathUtility.GetRelativePath(@"c:\root", value));
    }
}

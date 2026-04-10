using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathUtilityTests
{
    public static TheoryData<string, string> NormalizedPathScenarios =>
        new()
        {
            { @"C:\repo\project/bin\", @"C:\repo\project\bin" },
            { @"C:\repo/project\bin/", @"C:\repo\project\bin" },
            { "/home/alice/repo/project/bin/", "/home/alice/repo/project/bin" },
            { "/home/alice\\repo/project\\bin/", "/home/alice/repo/project/bin" },
            { "/Users/alice/repo/project/bin/", "/Users/alice/repo/project/bin" }
            ,{ "/Users/alice\\repo/project\\bin/", "/Users/alice/repo/project/bin" }
        };

    public static TheoryData<string, string> ParentPathScenarios =>
        new()
        {
            { @"C:\repo\project\bin\", @"C:\repo\project" },
            { @"C:\repo/project\bin/", @"C:\repo\project" },
            { "/home/alice/repo/project/bin/", "/home/alice/repo/project" },
            { "/home/alice\\repo/project\\bin/", "/home/alice/repo/project" },
            { "/Users/alice/repo/project/bin/", "/Users/alice/repo/project" }
            ,{ "/Users/alice\\repo/project\\bin/", "/Users/alice/repo/project" }
        };

    public static TheoryData<string, string, string> RelativePathScenarios =>
        new()
        {
            { @"C:\repo", @"C:\repo\project\bin", "project/bin" },
            { @"C:/repo", @"C:\repo/project\bin", "project/bin" },
            { "/home/alice/repo", "/home/alice/repo/project/bin", "project/bin" },
            { "/home/alice\\repo", "/home/alice/repo\\project/bin", "project/bin" },
            { "/Users/alice/repo", "/Users/alice/repo/project/bin", "project/bin" }
            ,{ "/Users/alice\\repo", "/Users/alice/repo\\project/bin", "project/bin" }
        };

    [Theory]
    [MemberData(nameof(NormalizedPathScenarios))]
    public void GetNormalizedPath_NormalizesRepresentativePlatformPaths(string path, string expectedPath)
    {
        // Act
        var result = PathUtility.GetNormalizedPath(path);

        // Assert
        Assert.Equal(PathUtility.GetNormalizedPath(expectedPath), result);
    }

    [Theory]
    [MemberData(nameof(NormalizedPathScenarios))]
    public void GetNormalizedPath_TrimsTrailingSeparatorForRepresentativePlatformPaths(string path, string _)
    {
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

    [Theory]
    [MemberData(nameof(ParentPathScenarios))]
    public void GetParentPath_ReturnsExpectedParentForRepresentativePlatformPaths(string path, string expectedParent)
    {
        // Act
        var result = PathUtility.GetParentPath(path);

        // Assert
        Assert.Equal(PathUtility.GetNormalizedPath(expectedParent), result);
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

    [Theory]
    [MemberData(nameof(RelativePathScenarios))]
    public void GetRelativePath_ReturnsForwardSlashRelativePathForRepresentativePlatformPaths(string rootPath, string path, string expectedRelativePath)
    {
        // Act
        var result = PathUtility.GetRelativePath(rootPath, path);

        // Assert
        Assert.Equal(expectedRelativePath, result);
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

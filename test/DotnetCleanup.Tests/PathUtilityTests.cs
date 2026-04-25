using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class PathUtilityTests
{
    public static TheoryData<PathScenario> RepresentativePathScenarios =>
        new()
        {
            new PathScenario(
                @"C:\repo",
                @"C:\repo\project/bin\",
                PlatformPath("C:|repo|project|bin"),
                PlatformPath("C:|repo|project"),
                "project/bin/"),
            new PathScenario(
                @"C:/repo",
                @"C:\repo/project\bin",
                PlatformPath("C:|repo|project|bin"),
                PlatformPath("C:|repo|project"),
                "project/bin"),
            new PathScenario(
                "/home/alice/repo",
                "/home/alice/repo/project/bin/",
                PlatformPath("|home|alice|repo|project|bin"),
                PlatformPath("|home|alice|repo|project"),
                "project/bin/"),
            new PathScenario(
                "/home/alice\\repo",
                "/home/alice/repo\\project/bin/",
                PlatformPath("|home|alice|repo|project|bin"),
                PlatformPath("|home|alice|repo|project"),
                "project/bin/")
        };

    [Theory]
    [MemberData(nameof(RepresentativePathScenarios))]
    public void GetNormalizedPath_NormalizesRepresentativePlatformPaths(PathScenario scenario)
    {
        // Act
        var result = PathUtility.GetNormalizedPath(scenario.Path);

        // Assert
        Assert.Equal(scenario.NormalizedPath, result);
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
    [MemberData(nameof(RepresentativePathScenarios))]
    public void GetParentPath_ReturnsExpectedParentForRepresentativePlatformPaths(PathScenario scenario)
    {
        // Act
        var result = PathUtility.GetParentPath(scenario.Path);

        // Assert
        Assert.Equal(scenario.ParentPath, result);
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
    public void GetParentPath_ReturnsExpectedParentForUncPathsOnWindows()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var uncPath = @"\\server\share\folder\child";

        // Act
        var result = PathUtility.GetParentPath(uncPath);

        // Assert
        Assert.Equal(@"\\server\share\folder", result);
    }

    [Theory]
    [MemberData(nameof(RepresentativePathScenarios))]
    public void GetRelativePath_ReturnsForwardSlashRelativePathForRepresentativePlatformPaths(PathScenario scenario)
    {
        // Act
        var result = PathUtility.GetRelativePath(scenario.RootPath, scenario.Path);

        // Assert
        Assert.Equal(scenario.RelativePath, result);
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

    private static string PlatformPath(string value)
    {
        return value.Replace('|', Path.DirectorySeparatorChar);
    }

    public sealed record PathScenario(
        string RootPath,
        string Path,
        string NormalizedPath,
        string ParentPath,
        string RelativePath);
}

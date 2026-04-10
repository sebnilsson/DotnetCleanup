using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupTempPathTests
{
    public static TheoryData<string> TempPathScenarios =>
        new()
        {
            { @"C:\temp\dotnetcleanup" },
            { @"C:/temp\dotnetcleanup" },
            { "/tmp/dotnetcleanup" },
            { "/tmp\\dotnetcleanup" },
            { "/private/tmp/dotnetcleanup" },
            { "/private/tmp\\dotnetcleanup" }
        };

    [Theory]
    [MemberData(nameof(TempPathScenarios))]
    public void CreatePath_AppendsDirectoryNameAndRelativeSegmentsForRepresentativePlatformPaths(string tempPath)
    {
        // Act
        var path = CleanupTempPath.CreatePath(
            tempPath,
            $"{CleanupTempPath.DirectoryNamePrefix}-test",
            "projectA",
            "bin");

        // Assert
        Assert.Equal(
            PathUtility.GetNormalizedPath(Path.Combine(tempPath, $"{CleanupTempPath.DirectoryNamePrefix}-test", "projectA", "bin")),
            path);
    }

    [Fact]
    public void GetRunDirectoryPrefix_ReturnsTempPathWithTimestampedPrefix()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2026, 04, 10, 14, 30, 45, TimeSpan.Zero);

        // Act
        var prefix = CleanupTempPath.GetRunDirectoryPrefix(TestPath.TempPath, startedAt);

        // Assert
        Assert.Equal(
            TestPath.Combine(TestPath.TempPath, $"{CleanupTempPath.DirectoryNamePrefix}-20260410-143045-"),
            prefix);
    }
}

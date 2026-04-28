using DotnetCleanup.IO;
using DotnetCleanup.Tests.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupTempPathTest
{
    public static TheoryData<TempPathScenario> TempPathScenarios =>
        new()
        {
            new TempPathScenario(
                @"C:\temp\dotnetcleanup",
                PlatformPath("C:|temp|dotnetcleanup|~dotnetcleanup-test|projectA|bin")),
            new TempPathScenario(
                @"C:/temp\dotnetcleanup",
                PlatformPath("C:|temp|dotnetcleanup|~dotnetcleanup-test|projectA|bin")),
            new TempPathScenario(
                "/tmp/dotnetcleanup",
                PlatformPath("|tmp|dotnetcleanup|~dotnetcleanup-test|projectA|bin")),
            new TempPathScenario(
                "/tmp\\dotnetcleanup",
                PlatformPath("|tmp|dotnetcleanup|~dotnetcleanup-test|projectA|bin"))
        };

    [Theory]
    [MemberData(nameof(TempPathScenarios))]
    public void CreatePath_AppendsDirectoryNameAndRelativeSegmentsForRepresentativePlatformPaths(TempPathScenario scenario)
    {
        // Act
        var path = CleanupTempPath.CreatePath(
            scenario.TempPath,
            $"{CleanupTempPath.DirectoryNamePrefix}-test",
            "projectA",
            "bin");

        // Assert
        Assert.Equal(scenario.ExpectedPath, path);
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
            CleanupTempPath.CreatePath(TestPath.TempPath, $"{CleanupTempPath.DirectoryNamePrefix}-20260410-143045-"),
            prefix);
    }

    private static string PlatformPath(string value)
    {
        return value.Replace('|', Path.DirectorySeparatorChar);
    }

    public sealed record TempPathScenario(string TempPath, string ExpectedPath);
}

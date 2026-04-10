using DotnetCleanup.IO;
using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupTempPathTests
{
    [Fact]
    public void CreatePath_AppendsDirectoryNameAndRelativeSegments()
    {
        // Arrange
        var tempPath = TestPath.TempPath;

        // Act
        var path = CleanupTempPath.CreatePath(
            tempPath,
            $"{CleanupTempPath.DirectoryNamePrefix}-test",
            "projectA",
            "bin");

        // Assert
        Assert.Equal(TestPath.Combine(tempPath, $"{CleanupTempPath.DirectoryNamePrefix}-test", "projectA", "bin"), path);
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

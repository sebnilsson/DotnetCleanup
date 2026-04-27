using DotnetCleanup.Tests.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupStepTests
{
    [Fact]
    public void AddSuccess_TreatsCaseInsensitivePathsAsEqual()
    {
        // Arrange
        var step = new CleanupStep();
        var lower = new PathInfo(TestPath.Root("folder", "bin"), isFile: false);
        var upper = new PathInfo(TestPath.Root("FOLDER", "BIN"), isFile: false);

        // Act
        step.AddSuccess(lower);
        var addedUpper = step.AddSuccess(upper);

        // Assert
        Assert.False(addedUpper);
        Assert.Single(step.Successes);
    }
}

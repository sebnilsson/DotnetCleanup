using DotnetCleanup.Testing.IO;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupStepTests
{
    [Fact]
    public void AddSuccess_AddsPathToSuccesses()
    {
        // Arrange
        var step = new CleanupStep();
        var path = new PathInfo(TestPath.Root("folder", "bin"), isFile: false);

        // Act
        var added = step.AddSuccess(path);

        // Assert
        Assert.True(added);
        Assert.Contains(path, step.Successes);
    }

    [Fact]
    public void AddSuccess_ReturnsFalseForDuplicatePath()
    {
        // Arrange
        var step = new CleanupStep();
        var path = new PathInfo(TestPath.Root("folder", "bin"), isFile: false);

        // Act
        step.AddSuccess(path);
        var addedAgain = step.AddSuccess(path);

        // Assert
        Assert.False(addedAgain);
        Assert.Single(step.Successes);
    }

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

    [Fact]
    public void AddFailed_AddsPathToFailed()
    {
        // Arrange
        var step = new CleanupStep();
        var path = new PathInfo(TestPath.Root("folder", "bin"), isFile: false);
        path.SetFailedOnMove(new IOException("fail"));

        // Act
        var added = step.AddFailed(path);

        // Assert
        Assert.True(added);
        Assert.Contains(path, step.Failed);
    }

    [Fact]
    public void AddFailed_ReturnsFalseForDuplicatePath()
    {
        // Arrange
        var step = new CleanupStep();
        var path = new PathInfo(TestPath.Root("folder", "bin"), isFile: false);
        path.SetFailedOnMove(new IOException("fail"));

        // Act
        step.AddFailed(path);
        var addedAgain = step.AddFailed(path);

        // Assert
        Assert.False(addedAgain);
        Assert.Single(step.Failed);
    }

    [Fact]
    public void NewStep_HasEmptyCollections()
    {
        // Arrange
        var step = new CleanupStep();

        // Assert
        Assert.Empty(step.Successes);
        Assert.Empty(step.Failed);
    }
}

using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupStepTests
{
    [Fact]
    public void AddSuccess_AddsPathToSuccesses()
    {
        var step = new CleanupStep();
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        var added = step.AddSuccess(path);

        Assert.True(added);
        Assert.Contains(path, step.Successes);
    }

    [Fact]
    public void AddSuccess_ReturnsFalseForDuplicatePath()
    {
        var step = new CleanupStep();
        var path = new PathInfo(@"c:\root\bin", isFile: false);

        step.AddSuccess(path);
        var addedAgain = step.AddSuccess(path);

        Assert.False(addedAgain);
        Assert.Single(step.Successes);
    }

    [Fact]
    public void AddSuccess_TreatsCaseInsensitivePathsAsEqual()
    {
        var step = new CleanupStep();
        var lower = new PathInfo(@"c:\root\bin", isFile: false);
        var upper = new PathInfo(@"C:\ROOT\BIN", isFile: false);

        step.AddSuccess(lower);
        var addedUpper = step.AddSuccess(upper);

        Assert.False(addedUpper);
        Assert.Single(step.Successes);
    }

    [Fact]
    public void AddFailed_AddsPathToFailed()
    {
        var step = new CleanupStep();
        var path = new PathInfo(@"c:\root\bin", isFile: false);
        path.SetFailedOnMove(new IOException("fail"));

        var added = step.AddFailed(path);

        Assert.True(added);
        Assert.Contains(path, step.Failed);
    }

    [Fact]
    public void AddFailed_ReturnsFalseForDuplicatePath()
    {
        var step = new CleanupStep();
        var path = new PathInfo(@"c:\root\bin", isFile: false);
        path.SetFailedOnMove(new IOException("fail"));

        step.AddFailed(path);
        var addedAgain = step.AddFailed(path);

        Assert.False(addedAgain);
        Assert.Single(step.Failed);
    }

    [Fact]
    public void NewStep_HasEmptyCollections()
    {
        var step = new CleanupStep();

        Assert.Empty(step.Successes);
        Assert.Empty(step.Failed);
    }
}

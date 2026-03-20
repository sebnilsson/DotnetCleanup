using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class HelperBehaviorTests
{
    [Fact]
    public void PathInfo_Constructor_NormalizesValueAndPreservesRawInput()
    {
        // Arrange
        var rawPath = $"folder{Path.AltDirectorySeparatorChar}child{Path.AltDirectorySeparatorChar}";

        // Act
        var pathInfo = new PathInfo(rawPath, isFile: false);

        // Assert
        var expectedValue = PathUtility.GetNormalizedPath(rawPath);

        Assert.Equal(rawPath, pathInfo.Raw);
        Assert.Equal(expectedValue, pathInfo.InitialValue);
        Assert.Equal(expectedValue, pathInfo.Value);
        Assert.False(pathInfo.IsFile);
    }

    [Fact]
    public void PathInfo_SetFailedOnMove_SetsFailureState()
    {
        // Arrange
        var pathInfo = new PathInfo("folder/child", isFile: true);
        var exception = new IOException("move failed");

        // Act
        pathInfo.SetFailedOnMove(exception);

        // Assert
        Assert.Same(exception, pathInfo.Exception);
        Assert.Equal(PathFailureStage.Move, pathInfo.FailedOn);
    }

    [Fact]
    public void PathUtility_GetParentPath_ReturnsExpectedParentsForNestedAndRootPaths()
    {
        // Arrange
        var nestedPath = Path.Combine(Path.GetTempPath(), "dotnetcleanup-tests", "bin");
        var rootPath = Path.GetPathRoot(Path.GetFullPath(nestedPath));

        // Act
        var nestedParent = PathUtility.GetParentPath(nestedPath);
        var rootParent = PathUtility.GetParentPath(rootPath);

        // Assert
        Assert.Equal(Path.GetDirectoryName(PathUtility.GetNormalizedPath(nestedPath)), nestedParent);
        Assert.Null(rootParent);
    }

    [Fact]
    public void PathUtility_GetParentPath_NormalizesMixedSeparatorsBeforeResolvingParent()
    {
        // Arrange
        var mixedSeparatorPath = $"alpha{Path.AltDirectorySeparatorChar}beta{Path.DirectorySeparatorChar}gamma";

        // Act
        var parent = PathUtility.GetParentPath(mixedSeparatorPath);

        // Assert
        Assert.Equal(PathUtility.GetNormalizedPath(Path.Combine("alpha", "beta")), parent);
    }

    [Fact]
    public void PathUtility_GetParentPath_ReturnsExpectedParentForUncPathsOnWindows()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var uncPath = @"\\server\share\folder\child";

        // Act
        var parent = PathUtility.GetParentPath(uncPath);

        // Assert
        Assert.Equal(@"\\server\share\folder", parent);
    }

    [Fact]
    public void CleanupStep_AddSuccess_DeduplicatesPathsCaseInsensitively()
    {
        // Arrange
        var step = new CleanupStep();
        var lowerCasePath = new PathInfo("folder/bin", isFile: false);
        var upperCasePath = new PathInfo("FOLDER/BIN", isFile: false);

        // Act
        var firstAddResult = step.AddSuccess(lowerCasePath);
        var secondAddResult = step.AddSuccess(upperCasePath);

        // Assert
        Assert.True(firstAddResult);
        Assert.False(secondAddResult);
        Assert.Single(step.Successes);
    }

    [Fact]
    public void SimpleTypeResolver_Resolve_IEnumerableReturnsAssignableRegistrations()
    {
        // Arrange
        var resolver = new SimpleTypeResolver(
            new Dictionary<Type, Func<SimpleTypeResolver, object>>
            {
                [typeof(AlphaHandler)] = _ => new AlphaHandler(),
                [typeof(BetaHandler)] = _ => new BetaHandler()
            });

        // Act
        var resolved = Assert.IsType<IHandler[]>(resolver.Resolve(typeof(IEnumerable<IHandler>)));

        // Assert
        Assert.Equal(2, resolved.Length);
        Assert.Contains(resolved, handler => handler is AlphaHandler);
        Assert.Contains(resolved, handler => handler is BetaHandler);
    }

    [Fact]
    public void SimpleTypeResolver_CreateInstance_UsesTheGreediestResolvableConstructor()
    {
        // Arrange
        var resolver = new SimpleTypeResolver(
            new Dictionary<Type, Func<SimpleTypeResolver, object>>
            {
                [typeof(IDependencyA)] = _ => new DependencyA(),
                [typeof(IDependencyB)] = _ => new DependencyB()
            });

        // Act
        var resolved = Assert.IsType<GreedyCtorType>(resolver.CreateInstance(typeof(GreedyCtorType)));

        // Assert
        Assert.NotNull(resolved.DependencyA);
        Assert.NotNull(resolved.DependencyB);
        Assert.True(resolved.UsedGreedyConstructor);
    }

    private interface IHandler;

    private sealed class AlphaHandler : IHandler;

    private sealed class BetaHandler : IHandler;

    private interface IDependencyA;

    private interface IDependencyB;

    private sealed class DependencyA : IDependencyA;

    private sealed class DependencyB : IDependencyB;

    private sealed class GreedyCtorType
    {
        public GreedyCtorType(IDependencyA dependencyA)
        {
            DependencyA = dependencyA;
            UsedGreedyConstructor = false;
        }

        public GreedyCtorType(IDependencyA dependencyA, IDependencyB dependencyB)
        {
            DependencyA = dependencyA;
            DependencyB = dependencyB;
            UsedGreedyConstructor = true;
        }

        public IDependencyA DependencyA { get; }

        public IDependencyB? DependencyB { get; }

        public bool UsedGreedyConstructor { get; }
    }
}

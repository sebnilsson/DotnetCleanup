using DotnetCleanup.Spectre;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class SimpleTypeResolverTests
{
    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var resolver = (SimpleTypeResolver)registrar.Build();

        // Act
        var resolved = resolver.Resolve(null);

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_RegisteredInstance_ReturnsSameInstance()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterInstance(typeof(ITestService), instance);
        var resolver = registrar.Build();

        // Act
        var resolved = resolver.Resolve(typeof(ITestService));

        // Assert
        Assert.Same(instance, resolved);
    }

    [Fact]
    public void Resolve_RegisteredType_CreatesInstance()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        registrar.Register(typeof(ITestService), typeof(TestService));
        var resolver = registrar.Build();

        // Act
        var resolved = resolver.Resolve(typeof(ITestService));

        // Assert
        Assert.IsType<TestService>(resolved);
    }

    [Fact]
    public void Resolve_RegisteredLazy_CallsFactory()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterLazy(typeof(ITestService), () => instance);
        var resolver = registrar.Build();

        // Act
        var resolved = resolver.Resolve(typeof(ITestService));

        // Assert
        Assert.Same(instance, resolved);
    }

    [Fact]
    public void Resolve_UnregisteredTypeWithParameterlessCtor_CreatesInstance()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var resolver = registrar.Build();

        // Act
        var resolved = resolver.Resolve(typeof(TestService));

        // Assert
        Assert.IsType<TestService>(resolved);
    }

    [Fact]
    public void Resolve_TypeWithDependency_InjectsRegisteredDependencies()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var dependency = new TestService();
        registrar.RegisterInstance(typeof(ITestService), dependency);
        var resolver = registrar.Build();

        // Act
        var resolved = Assert.IsType<TestConsumer>(resolver.Resolve(typeof(TestConsumer)));

        // Assert
        Assert.Same(dependency, resolved.Service);
    }

    [Fact]
    public void Resolve_IEnumerableOfRegisteredType_ReturnsArray()
    {
        // Arrange
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterInstance(typeof(TestService), instance);
        var resolver = registrar.Build();

        // Act
        var resolved = resolver.Resolve(typeof(IEnumerable<TestService>));

        // Assert
        var array = Assert.IsType<TestService[]>(resolved);
        Assert.Single(array);
        Assert.Same(instance, array[0]);
    }

    [Fact]
    public void Resolve_IEnumerableReturnsAssignableRegistrations()
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
    public void CreateInstance_UsesGreediestResolvableConstructor()
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

    public interface ITestService;

    public class TestService : ITestService;

    public class TestConsumer(SimpleTypeResolverTests.ITestService service)
    {
        public ITestService Service { get; } = service;
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

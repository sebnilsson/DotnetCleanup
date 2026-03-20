using DotnetCleanup.Spectre;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class SimpleTypeResolverTests
{
    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        var registrar = new SimpleTypeRegistrar();
        var resolver = (SimpleTypeResolver)registrar.Build();

        Assert.Null(resolver.Resolve(null));
    }

    [Fact]
    public void Resolve_RegisteredInstance_ReturnsSameInstance()
    {
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterInstance(typeof(ITestService), instance);
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(ITestService));

        Assert.Same(instance, resolved);
    }

    [Fact]
    public void Resolve_RegisteredType_CreatesInstance()
    {
        var registrar = new SimpleTypeRegistrar();
        registrar.Register(typeof(ITestService), typeof(TestService));
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(ITestService));

        Assert.IsType<TestService>(resolved);
    }

    [Fact]
    public void Resolve_RegisteredLazy_CallsFactory()
    {
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterLazy(typeof(ITestService), () => instance);
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(ITestService));

        Assert.Same(instance, resolved);
    }

    [Fact]
    public void Resolve_UnregisteredTypeWithParameterlessCtor_CreatesInstance()
    {
        var registrar = new SimpleTypeRegistrar();
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(TestService));

        Assert.IsType<TestService>(resolved);
    }

    [Fact]
    public void Resolve_TypeWithDependency_InjectsRegisteredDependencies()
    {
        var registrar = new SimpleTypeRegistrar();
        var dependency = new TestService();
        registrar.RegisterInstance(typeof(ITestService), dependency);
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(TestConsumer)) as TestConsumer;

        Assert.NotNull(resolved);
        Assert.Same(dependency, resolved!.Service);
    }

    [Fact]
    public void Resolve_IEnumerableOfRegisteredType_ReturnsArray()
    {
        var registrar = new SimpleTypeRegistrar();
        var instance = new TestService();
        registrar.RegisterInstance(typeof(TestService), instance);
        var resolver = registrar.Build();

        var resolved = resolver.Resolve(typeof(IEnumerable<TestService>));

        var array = Assert.IsType<TestService[]>(resolved);
        Assert.Single(array);
        Assert.Same(instance, array[0]);
    }

    public interface ITestService;

    public class TestService : ITestService;

    public class TestConsumer(SimpleTypeResolverTests.ITestService service)
    {
        public ITestService Service { get; } = service;
    }
}

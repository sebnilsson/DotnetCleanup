using Spectre.Console.Cli;

namespace DotnetCleanup.Spectre;

internal sealed class SimpleTypeRegistrar : ITypeRegistrar
{
    private readonly Dictionary<Type, Func<SimpleTypeResolver, object>> _registrations = new();

    public void Register(Type service, Type implementation)
    {
        _registrations[service] = resolver => resolver.CreateInstance(implementation)
            ?? throw new InvalidOperationException($"Failed to create {implementation}.");
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _registrations[service] = _ => implementation;
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _registrations[service] = _ => factory();
    }

    public ITypeResolver Build() => new SimpleTypeResolver(_registrations);
}

using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup;

internal static class CleanupApp
{
    internal static string[] NormalizeArgs(IEnumerable<string> args)
    {
        if (args == null)
        {
            return Array.Empty<string>();
        }

        return args.Select(arg => arg switch
        {
            "-nd" => "--no-delete",
            "-nm" => "--no-move",
            _ => arg
        }).ToArray();
    }

    public static CommandApp Build(
        IAnsiConsole console,
        ILogger<CleanupService> logger,
        IFileSystem fileSystem)
    {
        var registrar = new SimpleTypeRegistrar();
        registrar.RegisterInstance(typeof(IAnsiConsole), console);
        registrar.RegisterInstance(typeof(ILogger<CleanupService>), logger);
        registrar.RegisterInstance(typeof(IFileSystem), fileSystem);

        var app = new CommandApp(registrar);
        app.SetDefaultCommand<CleanupCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("cleanup");
            config.Settings.Console = console;
        });

        return app;
    }
}

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

    public ITypeResolver Build()
    {
        return new SimpleTypeResolver(_registrations);
    }
}

internal sealed class SimpleTypeResolver : ITypeResolver
{
    private readonly IReadOnlyDictionary<Type, Func<SimpleTypeResolver, object>> _registrations;

    public SimpleTypeResolver(IReadOnlyDictionary<Type, Func<SimpleTypeResolver, object>> registrations)
    {
        _registrations = registrations;
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var itemType = type.GetGenericArguments()[0];
            var matches = _registrations
                .Where(entry => itemType.IsAssignableFrom(entry.Key))
                .Select(entry => entry.Value(this))
                .ToArray();

            var array = Array.CreateInstance(itemType, matches.Length);
            for (var i = 0; i < matches.Length; i++)
            {
                array.SetValue(matches[i], i);
            }

            return array;
        }

        if (_registrations.TryGetValue(type, out var factory))
        {
            return factory(this);
        }

        return CreateInstance(type);
    }

    internal object? CreateInstance(Type type)
    {
        var constructors = type
            .GetConstructors()
            .OrderByDescending(ctor => ctor.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var arguments = new object?[parameters.Length];
            var canCreate = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var resolved = Resolve(parameters[i].ParameterType);
                if (resolved == null)
                {
                    canCreate = false;
                    break;
                }

                arguments[i] = resolved;
            }

            if (canCreate)
            {
                return constructor.Invoke(arguments);
            }
        }

        return Activator.CreateInstance(type);
    }

    public void Dispose()
    {
    }
}

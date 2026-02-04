using Spectre.Console.Cli;

namespace DotnetCleanup.Spectre;

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

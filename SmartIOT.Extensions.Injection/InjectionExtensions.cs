using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SmartIOT.Extensions.Injection;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
public static class InjectionExtensions
{
    /// <summary>
    /// This method injects fields marked with <see cref="InjectAttribute"/>
    /// and <see cref="InjectOptionalAttribute"/>, starting from the specified <paramref name="serviceProvider"/>.
    /// </summary>
    public static T? InjectDependencies<T>(this IServiceProvider serviceProvider, T? target)
    {
        serviceProvider.DoInjectDependencies(target, new Stack<Type>(), target?.GetType());

        return target;
    }

    private static void DoInjectDependencies(this IServiceProvider serviceProvider, object? target, Stack<Type> visitedTypes, Type? rootType)
    {
        if (target is null)
            return;

        var type = target.GetType();

        while (type is not null)
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => x.IsDefined(typeof(InjectAttribute), false) || x.IsDefined(typeof(InjectOptionalAttribute), false)))
            {
                if (visitedTypes.Contains(f.FieldType))
                    ThrowCircularDependenciesException(rootType!, f.FieldType, visitedTypes);

                visitedTypes.Push(f.FieldType);

                var plugin = f.GetCustomAttributes()
                    .Where(x => x.GetType() != typeof(InjectAttribute) && x.GetType() != typeof(InjectOptionalAttribute))
                    .Select(x => new { Attribute = x, Plugin = (IInjectionPlugin)serviceProvider.GetService(typeof(IInjectionPlugin<,>).MakeGenericType(x.GetType(), f.FieldType))! })
                    .FirstOrDefault(x => x.Plugin is not null);

                if (plugin is null)
                {
                    plugin = f.GetCustomAttributes()
                        .Where(x => x.GetType() != typeof(InjectAttribute) && x.GetType() != typeof(InjectOptionalAttribute))
                        .Select(x => new { Attribute = x, Plugin = (IInjectionPlugin)serviceProvider.GetService(typeof(IInjectionPlugin<>).MakeGenericType(x.GetType()))! })
                        .FirstOrDefault(x => x.Plugin is not null);
                }

                if (plugin is not null)
                {
                    var service = plugin.Plugin.Resolve(serviceProvider, plugin.Attribute, f.FieldType);
                    if (service is null)
                    {
                        if (f.IsDefined(typeof(InjectAttribute), false))
                            throw new InvalidOperationException($"Service type {f.FieldType.FullName} with attribute {plugin.Attribute.GetType().FullName} not found during injection");
                    }
                    else
                    {
                        serviceProvider.DoInjectDependencies(service, visitedTypes, rootType);
                        f.SetValue(target, service);
                    }
                }
                else if (f.IsDefined(typeof(InjectOptionalAttribute)))
                {
                    var service = serviceProvider.GetService(f.FieldType);
                    if (service is not null)
                    {
                        serviceProvider.DoInjectDependencies(service, visitedTypes, rootType);
                        f.SetValue(target, service);
                    }
                }
                else
                {
                    var service = serviceProvider.GetRequiredService(f.FieldType);
                    serviceProvider.DoInjectDependencies(service, visitedTypes, rootType);
                    f.SetValue(target, service);
                }

                _ = visitedTypes.Pop();
            }

#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            type = type.BaseType!;
        }
    }

    private static void ThrowCircularDependenciesException(Type rootType, Type type, Stack<Type> types)
    {
        throw new InvalidOperationException($"A circular dependency was detected while injecting services for type '{rootType.FullName}'.{Environment.NewLine}\t{type.FullName} -> {string.Join(" -> ", types.Select(x => x.FullName))}");
    }

    /// <summary>
    /// This method searches for the required service of the specified type and injects the fields.
    /// </summary>
    public static T GetRequiredServiceAndInject<T>(this IServiceProvider serviceProvider)
        where T : notnull
    {
        T service = serviceProvider.GetRequiredService<T>();

        serviceProvider.InjectDependencies(service);

        return service;
    }

    /// <summary>
    /// This method searches for the required service of the specified type and injects the fields.
    /// </summary>
    public static object GetRequiredServiceAndInject(this IServiceProvider serviceProvider, Type type)
    {
        var service = serviceProvider.GetRequiredService(type);

        return serviceProvider.InjectDependencies(service)!;
    }

    /// <summary>
    /// This method searches for the optional service of the specified type and injects the fields.
    /// </summary>
    public static T? GetServiceAndInject<T>(this IServiceProvider serviceProvider)
        where T : notnull
    {
        T? service = serviceProvider.GetService<T>();

        serviceProvider.InjectDependencies(service);

        return service;
    }

    /// <summary>
    /// This method searches for the optional service of the specified type and injects the fields.
    /// </summary>
    public static object? GetServiceAndInject(this IServiceProvider serviceProvider, Type type)
    {
        object? service = serviceProvider.GetService(type);

        return serviceProvider.InjectDependencies(service);
    }

    /// <summary>
    /// This method returns the list of services of the specified type, also injecting the fields.
    /// </summary>
    public static IEnumerable<object> GetServicesAndInject(this IServiceProvider serviceProvider, Type type)
    {
        IEnumerable<object?> services = serviceProvider.GetServices(type);

        return services.Select(x => serviceProvider.InjectDependencies(x))!;
    }

    /// <summary>
    /// This method returns the list of services of the specified type, also injecting the fields.
    /// </summary>
    public static IEnumerable<T> GetServicesAndInject<T>(this IServiceProvider serviceProvider)
    {
        IEnumerable<T> services = serviceProvider.GetServices<T>();

        return services.Select(x => serviceProvider.InjectDependencies(x)).Cast<T>()!;
    }
}

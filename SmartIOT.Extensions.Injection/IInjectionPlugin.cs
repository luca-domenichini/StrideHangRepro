namespace SmartIOT.Extensions.Injection;

#pragma warning disable S1133, CS0618

/// <summary>
/// For custom implementation, use IInjectionPlugin&lt;TAttribute, TTarget&gt; or IInjectionPlugin&lt;TAttribute&gt; instead.
/// This interface is used internally by the dependency injection system.
/// </summary>
public interface IInjectionPlugin
{
    /// <summary>
    /// Resolves an instance for the given attribute and target type
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="attribute">The attribute</param>
    /// <param name="targetType">The target type</param>
    /// <returns>An instance of the target type</returns>
    object? Resolve(IServiceProvider serviceProvider, Attribute attribute, Type targetType);
}

public interface IInjectionPlugin<in TAttribute> : IInjectionPlugin where TAttribute : Attribute
{
    /// <summary>
    /// Resolves an instance for the given attribute and target type
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="attribute">The attribute</param>
    /// <param name="targetType">The target type</param>
    /// <returns>An instance of the target type</returns>
    object? IInjectionPlugin.Resolve(IServiceProvider serviceProvider, Attribute attribute, Type targetType)
    {
        if (!attribute.GetType().IsAssignableTo(typeof(TAttribute)))
            throw new InvalidOperationException($"Error during injection of attribute {typeof(TAttribute).FullName}: Attribute type is wrong {attribute.GetType().FullName}");

        return Resolve(serviceProvider, (TAttribute)attribute, targetType);
    }

    /// <summary>
    /// Resolves an instance for the given attribute and target type
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="attribute">The attribute</param>
    /// <param name="targetType">The target type</param>
    /// <returns>An instance of the target type</returns>
    object? Resolve(IServiceProvider serviceProvider, TAttribute attribute, Type targetType);
}

public interface IInjectionPlugin<in TAttribute, out TTarget> : IInjectionPlugin where TAttribute : Attribute
{
    /// <summary>
    /// Resolves an instance for the given attribute and target type
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="attribute">The attribute</param>
    /// <param name="targetType">The target type</param>
    /// <returns>An instance of the target type</returns>
    object? IInjectionPlugin.Resolve(IServiceProvider serviceProvider, Attribute attribute, Type targetType)
    {
        if (!targetType.IsAssignableTo(typeof(TTarget)))
            throw new InvalidOperationException($"Error during injection of attribute {typeof(TAttribute).FullName}: Target type is not {typeof(TTarget).FullName}");
        if (!attribute.GetType().IsAssignableTo(typeof(TAttribute)))
            throw new InvalidOperationException($"Error during injection of attribute {typeof(TAttribute).FullName}: Attribute type is wrong {attribute.GetType().FullName}");

        return Resolve(serviceProvider, (TAttribute)attribute);
    }

    /// <summary>
    /// Resolves an instance for the given attribute
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="attribute">The attribute</param>
    /// <returns>An instance of the target type</returns>
    TTarget? Resolve(IServiceProvider serviceProvider, TAttribute attribute);
}

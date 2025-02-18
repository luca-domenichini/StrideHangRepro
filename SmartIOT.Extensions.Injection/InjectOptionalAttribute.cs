namespace SmartIOT.Extensions.Injection;

/// <summary>
/// This attribute is used to indicate that a field should be populated via injection from an <see cref="IServiceProvider"/>.
/// If the service is not registered in service provider, the field remains at its default value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectOptionalAttribute : Attribute
{
}

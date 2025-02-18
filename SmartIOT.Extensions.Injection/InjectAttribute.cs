namespace SmartIOT.Extensions.Injection;

/// <summary>
/// This attribute is used to indicate that a field must be populated via injection from an <see cref="IServiceProvider"/>.
/// If the service is not present, service provider should throw <see cref="InvalidOperationException"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectAttribute : Attribute
{
}

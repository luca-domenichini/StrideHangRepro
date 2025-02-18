using Stride.Engine;

namespace SmartIOT.Stride.Extensions;

public static class EntityComponentExtensions
{
    /// <summary>
    /// Gets the root parent entity of the given entity component.
    /// </summary>
    /// <param name="entityComponent">The entity component to get the root parent for.</param>
    /// <returns>The root parent entity.</returns>
    public static Entity GetRootEntity(this EntityComponent entityComponent)
    {
        return entityComponent.Entity.GetRootEntity();
    }
}

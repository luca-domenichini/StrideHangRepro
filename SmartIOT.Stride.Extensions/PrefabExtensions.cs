using Stride.Engine;

namespace SmartIOT.Stride.Extensions;

/// <summary>
/// Provides a set of extension methods for working with <see cref="Prefab"/> instances.
/// </summary>
public static class PrefabExtensions
{
    /// <summary>
    /// This method instantiate the prefab, wrapping the list of entities in a single root entity
    /// </summary>
    /// <param name="prefab"></param>
    public static Entity InstantiateWithRootEntity(this Prefab prefab)
    {
        List<Entity> entities = prefab.Instantiate();

        var root = new Entity();
        foreach (var entity in entities)
        {
            root.AddChild(entity);
        }

        return root;
    }
}

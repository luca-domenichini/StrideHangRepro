using Stride.Engine;

namespace SmartIOT.Stride.Extensions;

public static class EntityExtensions
{
    /// <summary>
    /// Recursively searches for a component of type T in the entity and its children.
    /// </summary>
    /// <typeparam name="T">The type of the component to search for.</typeparam>
    /// <param name="entity">The entity to search in.</param>
    /// <returns>The component of type T if found; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the entity is null.</exception>
    public static T? RecursiveGet<T>(this Entity entity) where T : EntityComponent
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var component = entity.Get<T>();
        if (component != null)
            return component;

        if (entity.Transform != null)
        {
            foreach (var child in entity.Transform.Children)
            {
                component = child.Entity.RecursiveGet<T>();
                if (component != null)
                    return component;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for a component of type T in the entity and its children.
    /// </summary>
    /// <typeparam name="T">The type of the component to search for.</typeparam>
    /// <param name="entity">The entity to search in.</param>
    /// <returns>The component of type T if found; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the entity is null.</exception>
    public static IEnumerable<T> RecursiveGetAll<T>(this Entity entity) where T : EntityComponent
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return DoRecursiveGetAll<T>(entity);
    }

    private static IEnumerable<T> DoRecursiveGetAll<T>(this Entity entity) where T : EntityComponent
    {
        var component = entity.Get<T>();
        if (component != null)
            yield return component;

        if (entity.Transform != null)
        {
            foreach (var child in entity.Transform.Children)
            {
                foreach (var item in child.Entity.DoRecursiveGetAll<T>())
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Gets the root parent of the entity.
    /// </summary>
    /// <param name="entity">The entity to get the root parent of.</param>
    /// <returns>The root parent entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the entity is null.</exception>
    public static Entity GetRootEntity(this Entity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var e = entity;
        Entity parent;
        while ((parent = e.GetParent()) != null)
            e = parent;

        return e;
    }
}

using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace SmartIOT.Stride.Extensions;

/// <summary>
/// Provides a set of extension methods for working with <see cref="CameraComponent"/> instances in a Bepu Physics context.
/// </summary>
public static class CameraBepuExtensions
{
    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through the mouse cursor position in screen coordinates,
    /// using input from the specified <see cref="ScriptComponent"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="component">The <see cref="ScriptComponent"/> from which the mouse position should be taken.</param>
    /// <param name="collisionMask">Optional. The collision mask to consider during the raycasting. Default is <see cref="CollisionMask.Everything"/>.</param>
    /// <returns>A <see cref="HitInfo"/> containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitInfo RaycastMouseBepu(this CameraComponent camera, ScriptComponent component, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var simulation = component.Entity.GetSimulation();
        if (simulation is null)
            throw new InvalidOperationException("Bepu Physics not initialized. Add at least one collider to the scene.");

        var backBuffer = component.GraphicsDevice.Presenter.BackBuffer;
        var viewPort = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
        var nearPosition = viewPort.Unproject(new Vector3(component.Input.AbsoluteMousePosition, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
        var farPosition = viewPort.Unproject(new Vector3(component.Input.AbsoluteMousePosition, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

        simulation.RayCast(nearPosition, farPosition, 1000, out HitInfo hit, collisionMask);
        return hit;
    }

    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through the mouse cursor position in screen coordinates,
    /// using input from the specified <see cref="ScriptComponent"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="component">The <see cref="ScriptComponent"/> from which the mouse position should be taken.</param>
    /// <param name="collisionMask">Optional. The collision mask to consider during the raycasting. Default is <see cref="CollisionMask.Everything"/>.</param>
    /// <returns>A <see cref="HitInfo"/> containing information about the hit result, including the hit location and other collision data.</returns>
    public static bool RaycastMouseBepu(this CameraComponent camera, ScriptComponent component, out HitInfo hitInfo, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var simulation = component.Entity.GetSimulation();
        if (simulation is null)
            throw new InvalidOperationException("Bepu Physics not initialized. Add at least one collider to the scene.");

        var backBuffer = component.GraphicsDevice.Presenter.BackBuffer;
        var viewPort = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
        var nearPosition = viewPort.Unproject(new Vector3(component.Input.AbsoluteMousePosition, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
        var farPosition = viewPort.Unproject(new Vector3(component.Input.AbsoluteMousePosition, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

        return simulation.RayCast(nearPosition, farPosition, 1000, out hitInfo, collisionMask);
    }
}

#pragma warning disable CS0649

using SmartIOT.Stride.Extensions;
using Stride.CommunityToolkit.Engine;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;

namespace SmartIOT.Stride.Library;

/// <summary>
/// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
/// </summary>
/// <remarks>
/// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
/// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
/// </remarks>
public class CameraController : SyncScript
{
    private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

    private Vector3 _upVector;
    private CameraComponent? _camera;
    private Vector3 _translation;
    private float _yaw;
    private float _pitch;
    private Vector3? _lookingAt;
    private float _lookingAtTime;
    private bool _rotating;
    private bool _rotationLocked;

    public bool Gamepad { get; set; } = false;

    public MouseButton RotationMouseButton { get; set; } = MouseButton.Right;

    public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

    public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

    public float SpeedFactor { get; set; } = 5.0f;

    public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

    public Vector2 MouseRotationSpeed { get; set; } = new Vector2(3.0f, 3.0f);

    public float MouseMovementSpeed { get; set; } = 0.3f;

    public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

    public float LookAtTime { get; set; } = 1.0f;

    public float MinimumY { get; set; } = 0.1f;

    public override void Start()
    {
        base.Start();

        // Default up-direction
        _upVector = Vector3.UnitY;

        _camera = Entity.Get<CameraComponent>() ?? throw new InvalidOperationException("CameraComponent was not found");

        // Configure touch input
        if (!Platform.IsWindowsDesktop)
        {
            Input.Gestures.Add(new GestureConfigDrag());
            Input.Gestures.Add(new GestureConfigComposite());
        }
    }

    public override void Update()
    {
        ProcessInput();
        UpdateTransform();
    }

    private void ProcessInput()
    {
        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        // LookAt animation
        if (_lookingAt.HasValue)
        {
            _lookingAtTime += deltaTime;
            _camera!.Entity.Transform.LookAt(_lookingAt.Value, Vector3.UnitY, deltaTime / LookAtTime);
        }
        if (_lookingAtTime >= LookAtTime)
        {
            _lookingAt = null;
            _lookingAtTime = 0;
        }

        _translation = Vector3.Zero;
        _yaw = 0f;
        _pitch = 0f;

        // Keyboard and Gamepad based movement
        HandleKeyboardAndGamepadMovement(deltaTime);

        // Keyboard and Gamepad based Rotation
        HandleKeyboardAndGamepadRotation(deltaTime);

        // Mouse movement and gestures
        HandleMouseAndTouchInput(deltaTime);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality", Justification = "Checking for exact 0 equality is ok")]
    private void HandleMouseAndTouchInput(float deltaTime)
    {
        // This type of input should not use delta time at all, they already are frame-rate independent.
        //    Lets say that you are going to move your finger/mouse for one second over 40 units, it doesn't matter
        //    the amount of frames occuring within that time frame, each frame will receive the right amount of delta:
        //    a quarter of a second -> 10 units, half a second -> 20 units, one second -> your 40 units.
        if (Input.HasMouse)
        {
            // Releasing middle mouse button, starts looking at the point where the mouse is pointing.
            // If mouse release happens after a drag, just ignore it.
            if (!_rotationLocked && Input.IsMouseButtonReleased(MouseButton.Middle))
            {
                if (_rotating)
                {
                    _rotating = false;
                    _yaw = 0;
                    _pitch = 0;
                }
                else
                {
                    // make the camera look at mouse position
                    if (_camera!.RaycastMouseBepu(this, out var result))
                    {
                        if (Input.IsKeyDown(Keys.LeftCtrl) || Input.IsKeyDown(Keys.RightCtrl))
                        {
                            _lookingAt = null;
                            _camera!.Entity.Transform.LookAt(result.Point, Vector3.UnitY, 1.0f);
                        }
                        else
                        {
                            _lookingAt = result.Point;
                            _camera!.Entity.Transform.LookAt(result.Point, Vector3.UnitY, deltaTime / LookAtTime);
                        }

                        _lookingAtTime = 0;
                    }
                }
            }

            // Rotate with defined mouse button or when rotation is locked
            if (Input.IsMouseButtonDown(RotationMouseButton) || _rotationLocked)
            {
                Input.LockMousePosition();
                Game.IsMouseVisible = false;

                _yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
                _pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
                _rotating = _rotating || _yaw != 0 || _pitch != 0;
                _rotationLocked = _rotationLocked || Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift);
            }
            else
            {
                Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
                _rotating = false;
            }

            if (Input.MouseWheelDelta != 0)
            {
                if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                    _translation.Z = Input.MouseWheelDelta * MouseMovementSpeed * SpeedFactor;
                else
                    _translation.Z = Input.MouseWheelDelta * MouseMovementSpeed;
            }

            // Handle gestures
            foreach (var gestureEvent in Input.GestureEvents)
            {
                switch (gestureEvent.Type)
                {
                    // Rotate by dragging
                    case GestureType.Drag:
                        var drag = (GestureEventDrag)gestureEvent;
                        var dragDistance = drag.DeltaTranslation;
                        _yaw = -dragDistance.X * TouchRotationSpeed.X;
                        _pitch = -dragDistance.Y * TouchRotationSpeed.Y;
                        break;

                    // Move along z-axis by scaling and in xy-plane by multi-touch dragging
                    case GestureType.Composite:
                        var composite = (GestureEventComposite)gestureEvent;
                        _translation.X = -composite.DeltaTranslation.X * TouchMovementSpeed.X;
                        _translation.Y = -composite.DeltaTranslation.Y * TouchMovementSpeed.Y;
                        _translation.Z = MathF.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
                        break;
                }
            }
        }

        // Reset looking at point and locked rotation when pressing escape
        if (Input.IsKeyPressed(Keys.Escape))
        {
            _lookingAt = null;
            _lookingAtTime = 0;
            _rotationLocked = false;
        }
    }

    private void HandleKeyboardAndGamepadRotation(float deltaTime)
    {
        // See Keyboard & Gamepad translation's deltaTime usage
        float speed = 1f * deltaTime;
        Vector2 rotation = Vector2.Zero;
        if (Gamepad && Input.HasGamePad)
        {
            GamePadState padState = Input.DefaultGamePad.State;
            rotation.X += padState.RightThumb.Y;
            rotation.Y += -padState.RightThumb.X;
        }

        if (Input.HasKeyboard)
        {
            if (Input.IsKeyDown(Keys.NumPad2))
            {
                rotation.X -= 1;
            }
            if (Input.IsKeyDown(Keys.NumPad8))
            {
                rotation.X += 1;
            }

            if (Input.IsKeyDown(Keys.NumPad4))
            {
                rotation.Y += 1;
            }
            if (Input.IsKeyDown(Keys.NumPad6))
            {
                rotation.Y -= 1;
            }

            // See Keyboard & Gamepad translation's Normalize() usage
            if (rotation.Length() > 1f)
            {
                rotation = Vector2.Normalize(rotation);
            }
        }

        // Modulate by speed
        rotation *= KeyboardRotationSpeed * speed;

        // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
        _pitch += rotation.X;
        _yaw += rotation.Y;
    }

    private void HandleKeyboardAndGamepadMovement(float deltaTime)
    {
        // Our base speed is: one unit per second:
        //    deltaTime contains the duration of the previous frame, let's say that in this update
        //    or frame it is equal to 1/60, that means that the previous update ran 1/60 of a second ago
        //    and the next will, in most cases, run in around 1/60 of a second from now. Knowing that,
        //    we can move 1/60 of a unit on this frame so that in around 60 frames(1 second)
        //    we will have travelled one whole unit in a second.
        //    If you don't use deltaTime your speed will be dependant on the amount of frames rendered
        //    on screen which often are inconsistent, meaning that if the player has performance issues,
        //    this entity will move around slower.
        float speed = 1f * deltaTime;

        Vector3 dir = Vector3.Zero;

        if (Gamepad && Input.HasGamePad)
        {
            GamePadState padState = Input.DefaultGamePad.State;
            // LeftThumb can be positive or negative on both axis (pushed to the right or to the left)
            dir.Z += padState.LeftThumb.Y;
            dir.X += padState.LeftThumb.X;

            // Triggers are always positive, in this case using one to increase and the other to decrease
            dir.Y -= padState.LeftTrigger;
            dir.Y += padState.RightTrigger;

            // Increase speed when pressing A, LeftShoulder or RightShoulder
            // Here:does the enum flag 'Buttons' has one of the flag ('A','LeftShoulder' or 'RightShoulder') set
            if ((padState.Buttons & (GamePadButton.A | GamePadButton.LeftShoulder | GamePadButton.RightShoulder)) != 0)
            {
                speed *= SpeedFactor;
            }
        }

        if (Input.HasKeyboard)
        {
            // Move with keyboard
            // Forward/Backward
            if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
            {
                dir.Z += 1;
            }
            if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
            {
                dir.Z -= 1;
            }

            // Left/Right
            if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
            {
                dir.X -= 1;
            }
            if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
            {
                dir.X += 1;
            }

            // Down/Up
            if (Input.IsKeyDown(Keys.Q))
            {
                dir.Y -= 1;
            }
            if (Input.IsKeyDown(Keys.E))
            {
                dir.Y += 1;
            }

            // Increase speed when pressing shift
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            {
                speed *= SpeedFactor;
            }

            // If the player pushes down two or more buttons, the direction and ultimately the base speed
            // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
            // normalizing the vector ensures that whichever direction the player chooses, that direction
            // will always be at most one unit in length.
            // We're keeping dir as is if isn't longer than one to retain sub unit movement:
            // a stick not entirely pushed forward should make the entity move slower.
            if (dir.Length() > 1f)
            {
                dir = Vector3.Normalize(dir);
            }
        }

        // Finally, push all of that to the translation variable which will be used within UpdateTransform()
        _translation += dir * KeyboardMovementSpeed * speed;
    }

    private void UpdateTransform()
    {
        // Get the local coordinate system
        var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

        // Enforce the global up-vector by adjusting the local x-axis
        var right = Vector3.Cross(rotation.Forward, _upVector);
        var up = Vector3.Cross(right, rotation.Forward);

        // Stabilize
        right.Normalize();
        up.Normalize();

        // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
        var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, _upVector));
        _pitch = MathUtil.Clamp(currentPitch + _pitch, -MaximumPitch, MaximumPitch) - currentPitch;

        Vector3 finalTranslation = _translation;
        finalTranslation.Z = -finalTranslation.Z;
        finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

        // Move in local coordinates
        Entity.Transform.Position += finalTranslation;
        // Make sure position is not below the floor
        if (Entity.Transform.Position.Y < MinimumY)
            Entity.Transform.Position.Y = MinimumY;

        // Yaw around global up-vector, pitch and roll in local space
        Entity.Transform.Rotation *= Quaternion.RotationAxis(right, _pitch) * Quaternion.RotationAxis(_upVector, _yaw);
    }
}

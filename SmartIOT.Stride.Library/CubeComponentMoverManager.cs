#pragma warning disable CS8618, CS0649

using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SmartIOT.Stride.Library;

public class CubeComponentMoverManager : SyncScript
{
    private sealed record CubeData
    {
        public CubeData(Entity entity, int currentSpherelId)
        {
            Entity = entity;
            CurrentSphereId = currentSpherelId;
        }

        public Entity Entity;
        public int CurrentSphereId;
        public int TargetSphereId;
        public bool Moving;
        public IReadOnlyList<WaypointComponent>? MovingWaypoints;
        public TimeSpan StartMovingTime = TimeSpan.Zero;
        public int NextWaypointIndex;
        public WaypointComponent? NextWaypoint;
        public Vector3 StartPosition = Vector3.Zero;
        public Quaternion StartRotation = Quaternion.Identity;
        public Vector3? EndPosition;
        public Quaternion? EndRotation;
        public TimeSpan AccumulatedMovingTime = TimeSpan.Zero;
        public TimeSpan TimeForNextWaypoint = TimeSpan.Zero;
    }

    private RoutingTable _routingTable;

    private readonly ConcurrentDictionary<int, CubeData> _cubes = new();
    private readonly ConcurrentDictionary<int, CubeData> _movingCubes = new();

    public void MoveOrCreateCube(int cubeId, int destinationSphereId)
    {
        if (!_cubes.TryGetValue(cubeId, out var cubeData))
        {
            var waypoint = _routingTable.GetStandbyWaypoint(destinationSphereId);
            if (waypoint != null)
            {
                var factory = Entity.Get<CubeEntityFactory>();
                var entity = factory.CreateCubeEntity(cubeId);

                cubeData = new CubeData(entity, destinationSphereId);
                _cubes.TryAdd(cubeId, cubeData);

                waypoint.Entity.Transform.UpdateWorldMatrix();
                waypoint.Entity.Transform.GetWorldTransformation(out var position, out var rotation, out _);

                cubeData.CurrentSphereId = cubeData.TargetSphereId = destinationSphereId;
                cubeData.Entity.Transform.SetWorld(position, rotation);

                SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            }
        }
        else if (cubeData.CurrentSphereId == cubeData.TargetSphereId && destinationSphereId != cubeData.TargetSphereId)
        {
            cubeData.TargetSphereId = destinationSphereId;

            // Notice that cubeData can already be in _movingCubes, because it's already moving.
            // In any case, we reset Moving flag to indicate the need to recalculate waypoints.
            cubeData.Moving = false;
            _movingCubes.TryAdd(cubeId, cubeData);
        }
    }

    public override void Start()
    {
        base.Start();

        _routingTable = Services.GetService<RoutingTable>();
    }

    public override void Update()
    {
        DebugText.Print($"Cubes:{_cubes.Count}, Moving:{_movingCubes.Count}", new Int2(100, 100));

        foreach (var pair in _movingCubes)
        {
            var arrived = MoveCube(pair.Value);
            if (arrived)
            {
                _movingCubes.TryRemove(pair.Key, out _);
                pair.Value.Moving = false;
            }
        }
    }

    /// <summary>
    /// This method returns true if cube is arrived at destination and should be discarded from "moving cubes".
    /// </summary>
    /// <param name="cube"></param>
    /// <returns></returns>
    private bool MoveCube(CubeData cube)
    {
        Entity cubeEntity = cube.Entity;

        if (!cube.Moving)
        {
            if (cube.TargetSphereId != 0 && cube.CurrentSphereId != cube.TargetSphereId)
            {
                cube.MovingWaypoints = _routingTable.GetWaypoints(cube.CurrentSphereId, cube.TargetSphereId);
                if (cube.MovingWaypoints.Count > 0)
                {
                    cube.StartMovingTime = Game.UpdateTime.Total;
                    cube.AccumulatedMovingTime = TimeSpan.Zero;

                    cube.StartPosition = cubeEntity.Transform.Position;
                    cube.StartRotation = cubeEntity.Transform.Rotation;
                    cube.NextWaypointIndex = 0;
                    cube.NextWaypoint = cube.MovingWaypoints[0];
                    cube.NextWaypoint.Entity.Transform.GetWorldTransformation(out var waypointPosition, out var waypointRotation, out _);
                    cube.EndPosition = waypointPosition;
                    cube.EndRotation = waypointRotation;

                    var timeForWholeMovement = _routingTable.GetAvgMovementTime(cube.CurrentSphereId, cube.TargetSphereId);
                    if (timeForWholeMovement == TimeSpan.Zero)
                        timeForWholeMovement = TimeSpan.FromMilliseconds(500);
                    cube.TimeForNextWaypoint = timeForWholeMovement / cube.MovingWaypoints!.Count;

                    cube.Moving = true;

                    // Teturn immediately: cube is setup to move starting from next frame.
                    // This is correct, otherwise the cube would already be moving for a time equal to Game.UpdateTime.Elapsed, whereas the movement starts in the current frame.
                    return false;
                }
            }

            // no waypoint found to move cube, or cube already at destination
            return true;
        }

        // Get next waypoint position and rotation, and interpolate between start position/rotation and waypoint position/rotation.
        cube.AccumulatedMovingTime += Game.UpdateTime.Elapsed;
        var elapsedPercent = Math.Min(1, cube.AccumulatedMovingTime / cube.TimeForNextWaypoint);

        // if waypoint is reached, pop it from list.
        if (elapsedPercent >= 1)
        {
            cubeEntity.Transform.Position = cube.StartPosition = cube.EndPosition!.Value;
            cubeEntity.Transform.Rotation = cube.StartRotation = cube.EndRotation!.Value;

            cube.AccumulatedMovingTime = TimeSpan.Zero;

            if (cube.NextWaypointIndex >= cube.MovingWaypoints!.Count - 1)
            {
                // position reached!
                _routingTable.AddTimeSample(cube.CurrentSphereId, cube.TargetSphereId, Game.UpdateTime.Total - cube.StartMovingTime);

                cube.Moving = false;
                cube.CurrentSphereId = cube.TargetSphereId;
            }
            else
            {
                // next waypoint still to reach
                cube.NextWaypointIndex++;
                cube.NextWaypoint = cube.MovingWaypoints![cube.NextWaypointIndex];
                cube.NextWaypoint.Entity.Transform.GetWorldTransformation(out var waypointPosition, out var waypointRotation, out _);
                cube.EndPosition = waypointPosition;
                cube.EndRotation = waypointRotation;
            }
        }
        else
        {
            // apply position/rotation to cubeEntity
            cubeEntity.Transform.Position = Vector3.Lerp(cube.StartPosition, cube.EndPosition!.Value, (float)elapsedPercent);
            cubeEntity.Transform.Rotation = Quaternion.Lerp(cube.StartRotation, cube.EndRotation!.Value, (float)elapsedPercent);
        }

        // Return true if cube has finished moving
        return !cube.Moving;
    }
}

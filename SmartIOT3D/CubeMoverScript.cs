#pragma warning disable CS8618, CS0649

using SmartIOT.Stride.Library;
using Stride.Engine;
using System;

namespace SmartIOT3D;

public class CubeMoverScript : SyncScript
{
    private TimeSpan _lastMove = TimeSpan.Zero;

    public int CubeCount { get; set; } = 50;

    public Prefab WaypointPrefab { get; set; }

    private RoutingTable _routingTable;

    private CubeComponentMoverManager _manager;

    public override void Start()
    {
        base.Start();

        _routingTable = Services.GetService<RoutingTable>();

        _manager = Entity.Get<CubeComponentMoverManager>();
    }

    public override void Update()
    {
        if (Game.UpdateTime.Total - _lastMove > TimeSpan.FromMilliseconds(500))
        {
            var count = Random.Shared.NextDouble() * CubeCount / 2;
            for (int i = 0; i < count; i++)
            {
                MoveRandomCube();
            }

            _lastMove = Game.UpdateTime.Total;
        }
    }

    private void MoveRandomCube()
    {
        int id = Random.Shared.Next(1, CubeCount + 1);

        MoveCubeRandomly(id);
    }

    private void MoveCubeRandomly(int cubeId)
    {
        var destination = _routingTable.GetRandomSphereId();
        _manager!.MoveOrCreateCube(cubeId, destination);
    }
}

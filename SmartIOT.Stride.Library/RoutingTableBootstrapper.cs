#pragma warning disable CS8618, CS0649

using SmartIOT.Stride.Extensions;
using Stride.Engine;

namespace SmartIOT.Stride.Library;

public class RoutingTableBootstrapper : StartupScript
{
    public override void Start()
    {
        base.Start();

        var routingTable = Services.GetService<RoutingTable>();
        var id = 0;

        // explore all Entities to find attached waypoints. Populate routing table with them.
        foreach (var e in SceneSystem.SceneInstance.RootScene.Entities)
        {
            var sphere = e.RecursiveGet<SphereComponent>();
            if (sphere != null)
            {
                id++;
                sphere.SphereId = id;

                // search waypoints in entity children
                foreach (var waypoint in e.RecursiveGetAll<WaypointComponent>())
                {
                    routingTable!.AddEntry(id, waypoint);
                }
            }
        }
    }
}

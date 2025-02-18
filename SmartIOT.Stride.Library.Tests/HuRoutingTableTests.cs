namespace SmartIOT.Stride.Library.Tests;

public class HuRoutingTableTests
{
    [Fact]
    public void Test_Waypoint_retrieval()
    {
        var wp1 = new HuWaypointComponent();
        var wp2 = new HuWaypointComponent();
        var wp3in = new HuWaypointComponent()
        {
            HuWaypointType = HuWaypointType.TraversalIn
        };
        var wp3 = new HuWaypointComponent();
        var wp3out = new HuWaypointComponent()
        {
            HuWaypointType = HuWaypointType.TraversalOut
        };
        var wp4in = new HuWaypointComponent()
        {
            HuWaypointType = HuWaypointType.TraversalIn,
            TraversalAutomationModelIds = [99]
        };
        var wp4 = new HuWaypointComponent();
        var wp4out = new HuWaypointComponent()
        {
            HuWaypointType = HuWaypointType.TraversalOut,
            TraversalAutomationModelIds = [99]
        };
        var wp5 = new HuWaypointComponent();

        var rt = new HuRoutingTable();
        rt.AddEntry(1, wp1);
        rt.AddEntry(2, wp2);
        rt.AddEntry(3, wp3in);
        rt.AddEntry(3, wp3);
        rt.AddEntry(3, wp3out);
        rt.AddEntry(4, wp4in);
        rt.AddEntry(4, wp4);
        rt.AddEntry(4, wp4out);
        rt.AddEntry(5, wp5);

        Assert.Equal(wp1, rt.GetStandbyWaypoint(1));
        Assert.Equal(wp2, rt.GetStandbyWaypoint(2));
        Assert.Equal(wp3, rt.GetStandbyWaypoint(3));
        Assert.Equal(wp4, rt.GetStandbyWaypoint(4));
        Assert.Equal(wp5, rt.GetStandbyWaypoint(5));

        // 1 -> 2
        var list = rt.GetWaypoints(1, 2);
        Assert.Single(list);
        Assert.Equal(wp2, list[0]);

        // 2 -> 3
        list = rt.GetWaypoints(2, 3);
        Assert.Equal(2, list.Count);
        Assert.Equal(wp3in, list[0]);
        Assert.Equal(wp3, list[1]);

        // 3 -> 4
        list = rt.GetWaypoints(3, 4);
        Assert.Equal(2, list.Count);
        Assert.Equal(wp3out, list[0]);
        Assert.Equal(wp4, list[1]);

        // 4 -> 5
        list = rt.GetWaypoints(4, 5);
        Assert.Single(list);
        Assert.Equal(wp5, list[0]);
    }
}
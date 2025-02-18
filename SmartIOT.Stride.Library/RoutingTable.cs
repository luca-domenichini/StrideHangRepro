using Stride.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartIOT.Stride.Library;

/// <summary>
/// RoutingTable is a class that represents a routing table.
/// It contains a list of <see cref="RoutingTableEntry"/>.
/// </summary>
public class RoutingTable
{
    /// <summary>
    /// RoutingTableEntry is a class that represents an x in the routing table.
    /// </summary>
    private sealed record RoutingTableEntry
    {
        /// <summary>
        /// OwnerSphereId is the owner SphereId of this entry, and is the model to which the entry belongs to.
        /// </summary>
        public int OwnerSphereId { get; }

        /// <summary>
        /// The Waypoint of the x.
        /// </summary>
        public WaypointComponent Waypoint { get; }

        public RoutingTableEntry(int ownerSphereId, WaypointComponent waypoint)
        {
            OwnerSphereId = ownerSphereId;
            Waypoint = waypoint ?? throw new ArgumentNullException(nameof(waypoint));
        }
    }

    private sealed record TransitionTime
    {
        private int _count;
        private TimeSpan _totalTime;
        public TimeSpan AvgTime => _totalTime / _count;

        public void AddTimeSample(TimeSpan time)
        {
            if (_count < 100)
            {
                _totalTime += time;
                _count++;
            }
            else
            {
                var avg = _totalTime / _count;
                _totalTime = avg * 99 + time;
            }
        }
    }

    private readonly Dictionary<int, List<RoutingTableEntry>> _entries = new();
    private readonly Dictionary<int, WaypointComponent> _standbyWaypoints = new();
    private readonly ConcurrentDictionary<(int, int), TransitionTime> _times = new();
    private readonly ConcurrentDictionary<(int, int), List<WaypointComponent>> _cache = new();

    public void AddEntry(int ownerSphereId, WaypointComponent waypoint)
    {
        var entry = new RoutingTableEntry(ownerSphereId, waypoint);

        _entries.GetOrCreateValue(entry.OwnerSphereId).Add(entry);
        _standbyWaypoints.TryAdd(entry.OwnerSphereId, entry.Waypoint);
    }

    public void RemoveEntry(int ownerSphereId, WaypointComponent waypoint)
    {
        var entry = new RoutingTableEntry(ownerSphereId, waypoint);

        if (_entries.TryGetValue(entry.OwnerSphereId, out var list))
            list.Remove(entry);

        if (_standbyWaypoints.TryGetValue(entry.OwnerSphereId, out var wp)
                && wp == entry.Waypoint)
            _standbyWaypoints.Remove(entry.OwnerSphereId);
    }

    public WaypointComponent? GetStandbyWaypoint(int destinationSphereId)
    {
        if (_standbyWaypoints.TryGetValue(destinationSphereId, out var waypoint))
            return waypoint;

        return null;
    }

    public IReadOnlyList<WaypointComponent> GetWaypoints(int sourceSphereId, int destinationSphereId)
    {
        if (_cache.TryGetValue((sourceSphereId, destinationSphereId), out var list))
            return list;

        list = new();

        if (_standbyWaypoints.TryGetValue(destinationSphereId, out var waypointComponent))
            list.Add(waypointComponent);

        // Try to add the list to the cache, since another thread might have already inserted it
        _cache.TryAdd((sourceSphereId, destinationSphereId), list);

        return list;
    }

    public void AddTimeSample(int sourceSphereId, int destinationSphereId, TimeSpan time)
    {
        var item = _times.GetOrAdd((sourceSphereId, destinationSphereId), (_) => new TransitionTime());
        item.AddTimeSample(time);
    }

    public TimeSpan GetAvgMovementTime(int sourceSphereId, int destinationSphereId)
    {
        if (_times.TryGetValue((sourceSphereId, destinationSphereId), out var time))
            return time.AvgTime;

        return TimeSpan.Zero;
    }

    public int GetRandomSphereId()
    {
        return _entries.Keys.Skip(Random.Shared.Next(1, _entries.Keys.Count + 1) - 1).First();
    }
}

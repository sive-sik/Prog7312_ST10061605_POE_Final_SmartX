using SmartX.Core.DataStructures;
using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class MeshNetworkStore
{
    private readonly object sync = new();
    private readonly MeshNetworkGraph graph = new();
    private readonly HashSet<Guid> offlineNodes = new();

    public void SynchroniseDevices(IEnumerable<SensorDevice> devices)
    {
        lock (sync)
        {
            foreach (SensorDevice device in devices)
            {
                graph.AddNode(device.Id);
                if (device.Status.Equals("Offline", StringComparison.OrdinalIgnoreCase)
                    || device.Status.Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    offlineNodes.Add(device.Id);
                }
            }
        }
    }

    public void AddLink(Guid fromId, Guid toId, double weight)
    {
        lock (sync)
        {
            if (!graph.Nodes.Contains(fromId) || !graph.Nodes.Contains(toId))
            {
                throw new ArgumentException("Both devices must be registered before adding a link.");
            }
            graph.AddLink(fromId, toId, weight);
        }
    }

    public void SetOffline(Guid deviceId, bool offline)
    {
        lock (sync)
        {
            if (!graph.Nodes.Contains(deviceId)) { throw new ArgumentException("Device not found."); }
            if (offline) { offlineNodes.Add(deviceId); }
            else { offlineNodes.Remove(deviceId); }
        }
    }

    public object GetTopology()
    {
        lock (sync)
        {
            return new
            {
                NodeIds = graph.Nodes.ToList(),
                Links = graph.GetLinks(),
                OfflineNodeIds = offlineNodes.ToList(),
                MinimumSpanningTree = graph.MinimumSpanningTree(offlineNodes)
            };
        }
    }

    public object GetRoute(Guid fromId, Guid toId)
    {
        lock (sync)
        {
            List<Guid> path = graph.FindRoute(fromId, toId, offlineNodes);
            return new { Path = path, Reachable = path.Count > 0, HopCount = Math.Max(0, path.Count - 1) };
        }
    }

    public object GetConnectivity(Guid fromId)
    {
        lock (sync)
        {
            return new { ReachableNodeIds = graph.BreadthFirstSearch(fromId, offlineNodes) };
        }
    }
}

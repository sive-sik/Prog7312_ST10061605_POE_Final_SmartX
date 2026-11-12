using SmartX.Core.DataStructures;
using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class MeshNetworkStore
{
    private readonly object sync = new();
    private readonly MeshNetworkGraph graph = new();
    private readonly HashSet<Guid> reportedOfflineNodes = new();
    private readonly Dictionary<Guid, bool> simulatedOfflineOverrides = new();

    private HashSet<Guid> EffectiveOfflineNodes()
    {
        HashSet<Guid> result = new(reportedOfflineNodes);
        foreach (var status in simulatedOfflineOverrides)
        {
            if (status.Value) { result.Add(status.Key); }
            else { result.Remove(status.Key); }
        }
        return result;
    }

    public void SynchroniseDevices(IEnumerable<SensorDevice> devices)
    {
        lock (sync)
        {
            reportedOfflineNodes.Clear();
            foreach (SensorDevice device in devices)
            {
                graph.AddNode(device.Id);
                if (device.Status.Equals("Offline", StringComparison.OrdinalIgnoreCase)
                    || device.Status.Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    reportedOfflineNodes.Add(device.Id);
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
            simulatedOfflineOverrides[deviceId] = offline;
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
                OfflineNodeIds = EffectiveOfflineNodes().ToList(),
                MinimumSpanningTree = graph.MinimumSpanningTree(EffectiveOfflineNodes())
            };
        }
    }

    public object GetRoute(Guid fromId, Guid toId)
    {
        lock (sync)
        {
            List<Guid> path = graph.FindRoute(fromId, toId, EffectiveOfflineNodes());
            return new { Path = path, Reachable = path.Count > 0, HopCount = Math.Max(0, path.Count - 1) };
        }
    }

    public object GetConnectivity(Guid fromId)
    {
        lock (sync)
        {
            return new { ReachableNodeIds = graph.BreadthFirstSearch(fromId, EffectiveOfflineNodes()) };
        }
    }
}

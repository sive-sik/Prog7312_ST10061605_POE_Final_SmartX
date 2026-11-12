using SmartX.Core.Models;

namespace SmartX.Core.DataStructures;

public class MeshNetworkGraph
{
    private readonly Dictionary<Guid, Dictionary<Guid, double>> connections = new();

    public IReadOnlyCollection<Guid> Nodes => connections.Keys;

    public void AddNode(Guid id)
    {
        connections.TryAdd(id, new Dictionary<Guid, double>());
    }

    public void AddLink(Guid fromId, Guid toId, double weight)
    {
        if (fromId == toId || !double.IsFinite(weight) || weight <= 0)
        {
            throw new ArgumentException("A link must connect different nodes with a positive finite weight.");
        }

        AddNode(fromId);
        AddNode(toId);
        connections[fromId][toId] = weight;
        connections[toId][fromId] = weight;
    }

    public List<MeshLink> GetLinks()
    {
        List<MeshLink> links = new();
        HashSet<(Guid, Guid)> visited = new();
        foreach (var node in connections)
        {
            foreach (var neighbour in node.Value)
            {
                if (visited.Contains((neighbour.Key, node.Key))) { continue; }
                visited.Add((node.Key, neighbour.Key));
                links.Add(new MeshLink { FromId = node.Key, ToId = neighbour.Key, Weight = neighbour.Value });
            }
        }
        return links;
    }

    public List<Guid> BreadthFirstSearch(Guid startId, ISet<Guid>? offlineNodes = null)
    {
        List<Guid> visited = new();
        if (!connections.ContainsKey(startId) || offlineNodes?.Contains(startId) == true) { return visited; }

        HashSet<Guid> seen = new() { startId };
        Queue<Guid> queue = new();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            visited.Add(current);
            foreach (Guid neighbour in connections[current].Keys.OrderBy(id => id))
            {
                if (offlineNodes?.Contains(neighbour) == true || !seen.Add(neighbour)) { continue; }
                queue.Enqueue(neighbour);
            }
        }

        return visited;
    }

    public List<Guid> FindRoute(Guid startId, Guid endId, ISet<Guid>? offlineNodes = null)
    {
        if (!connections.ContainsKey(startId) || !connections.ContainsKey(endId)
            || offlineNodes?.Contains(startId) == true || offlineNodes?.Contains(endId) == true)
        {
            return new();
        }

        Queue<Guid> queue = new();
        Dictionary<Guid, Guid?> parents = new() { [startId] = null };
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            if (current == endId) { break; }
            foreach (Guid neighbour in connections[current].Keys.OrderBy(id => id))
            {
                if (offlineNodes?.Contains(neighbour) == true || parents.ContainsKey(neighbour)) { continue; }
                parents[neighbour] = current;
                queue.Enqueue(neighbour);
            }
        }

        if (!parents.ContainsKey(endId)) { return new(); }
        List<Guid> route = new();
        Guid? step = endId;
        while (step.HasValue)
        {
            route.Add(step.Value);
            step = parents[step.Value];
        }
        route.Reverse();
        return route;
    }

    public List<MeshLink> MinimumSpanningTree(ISet<Guid>? offlineNodes = null)
    {
        List<MeshLink> result = new();
        HashSet<Guid> visited = new();

        // Prim's algorithm builds a minimum spanning forest if the mesh is disconnected.
        foreach (Guid startId in connections.Keys.OrderBy(id => id))
        {
            if (visited.Contains(startId) || offlineNodes?.Contains(startId) == true) { continue; }
            visited.Add(startId);
            PriorityQueue<MeshLink, double> queue = new();
            AddEdges(startId, queue);

            while (queue.TryDequeue(out MeshLink? link, out _))
            {
                if (visited.Contains(link.ToId) || offlineNodes?.Contains(link.ToId) == true) { continue; }
                visited.Add(link.ToId);
                result.Add(link);
                AddEdges(link.ToId, queue);
            }

            void AddEdges(Guid fromId, PriorityQueue<MeshLink, double> pending)
            {
                foreach (var neighbour in connections[fromId])
                {
                    if (visited.Contains(neighbour.Key) || offlineNodes?.Contains(neighbour.Key) == true) { continue; }
                    pending.Enqueue(new MeshLink
                    {
                        FromId = fromId,
                        ToId = neighbour.Key,
                        Weight = neighbour.Value
                    }, neighbour.Value);
                }
            }
        }

        return result;
    }
}

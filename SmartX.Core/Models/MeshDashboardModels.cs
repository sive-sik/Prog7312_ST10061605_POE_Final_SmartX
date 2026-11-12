using SmartX.Core.Models;

namespace SmartX.Core.Models;

public class MeshTopologySnapshot
{
    public List<Guid> NodeIds { get; set; } = new();
    public List<MeshLink> Links { get; set; } = new();
    public List<Guid> OfflineNodeIds { get; set; } = new();
    public List<MeshLink> MinimumSpanningTree { get; set; } = new();
}

public class MeshRouteResult
{
    public List<Guid> Path { get; set; } = new();
    public bool Reachable { get; set; }
    public int HopCount { get; set; }
}

public class MeshConnectivityResult
{
    public List<Guid> ReachableNodeIds { get; set; } = new();
}

public class MeshLinkRequest
{
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public double Weight { get; set; }
}

public class MeshStatusRequest
{
    public bool Offline { get; set; }
}

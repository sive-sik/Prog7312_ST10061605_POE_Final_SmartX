namespace SmartX.Core.Models;

public class MeshLink
{
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public double Weight { get; set; }
}

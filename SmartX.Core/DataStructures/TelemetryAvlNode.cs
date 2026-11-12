using SmartX.Core.Models;

namespace SmartX.Core.DataStructures;

public class TelemetryAvlNode
{
    public DateTime Timestamp { get; set; }
    public List<StreamTelemetryItem> Readings { get; } = new();
    public TelemetryAvlNode? Left { get; set; }
    public TelemetryAvlNode? Right { get; set; }
    public int Height { get; set; } = 1;

    public TelemetryAvlNode(StreamTelemetryItem reading)
    {
        Timestamp = reading.Timestamp;
        Readings.Add(reading);
    }
}

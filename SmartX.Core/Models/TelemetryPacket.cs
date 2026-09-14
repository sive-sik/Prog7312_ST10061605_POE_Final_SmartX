namespace SmartX.Core.Models;

public class TelemetryPacket<T>
{
    public string DeviceId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Metric { get; set; } = string.Empty;

    public T? Value { get; set; }

    public string Unit { get; set; } = string.Empty;
}
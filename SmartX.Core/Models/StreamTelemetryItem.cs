namespace SmartX.Core.Models;

public class StreamTelemetryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SensorId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsCritical { get; set; }
    public string AlertState { get; set; } = "Normal";
}

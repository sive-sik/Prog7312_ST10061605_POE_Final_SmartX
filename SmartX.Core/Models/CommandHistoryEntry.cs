namespace SmartX.Core.Models;

public class CommandHistoryEntry
{
    public Guid CommandId { get; set; }
    public Guid SensorId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
}

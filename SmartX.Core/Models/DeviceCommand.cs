namespace SmartX.Core.Models;

public class DeviceCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SensorId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string PreviousCommand { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public bool WasUndone { get; set; }
}

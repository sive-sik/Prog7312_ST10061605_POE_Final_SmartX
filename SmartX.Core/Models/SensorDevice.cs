namespace SmartX.Core.Models;

public class SensorDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string MacAddress { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = "Online";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
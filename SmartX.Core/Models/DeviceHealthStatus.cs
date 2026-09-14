namespace SmartX.Core.Models;

public class DeviceHealthStatus
{
    public Guid SensorId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public string LatestReading { get; set; } = string.Empty;

    public DateTime? LastContact { get; set; }

    public string Status { get; set; } = "Healthy";

    public string Recommendation { get; set; } = string.Empty;
}
namespace SmartX.Core.Models;

public class SuggestedAction
{
    public Guid SensorId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SuggestedCommand { get; set; } = string.Empty;
    public int Score { get; set; }
}

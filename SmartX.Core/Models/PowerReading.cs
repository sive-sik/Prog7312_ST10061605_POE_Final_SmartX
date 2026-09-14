namespace SmartX.Core.Models;

public class PowerReading
{
    public string DeviceId { get; set; } = string.Empty;

    public double Watts { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static PowerReading operator +(PowerReading left, PowerReading right)
    {
        return new PowerReading
        {
            DeviceId = $"{left.DeviceId}+{right.DeviceId}",
            Watts = left.Watts + right.Watts,
            Timestamp = DateTime.UtcNow
        };
    }

    public static PowerReading operator -(PowerReading left, PowerReading right)
    {
        return new PowerReading
        {
            DeviceId = $"{left.DeviceId}-{right.DeviceId}",
            Watts = left.Watts - right.Watts,
            Timestamp = DateTime.UtcNow
        };
    }
}
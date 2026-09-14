using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class TelemetryStore
{
    public List<TelemetryPacket<double>> DoubleTelemetry { get; } = new();

    public List<TelemetryPacket<int>> IntegerTelemetry { get; } = new();

    public List<TelemetryPacket<bool>> BooleanTelemetry { get; } = new();

    public void Add(TelemetryPacket<double> packet)
    {
        DoubleTelemetry.Add(packet);
    }

    public void Add(TelemetryPacket<int> packet)
    {
        IntegerTelemetry.Add(packet);
    }

    public void Add(TelemetryPacket<bool> packet)
    {
        BooleanTelemetry.Add(packet);
    }
}
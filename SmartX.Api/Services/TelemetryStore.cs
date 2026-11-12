using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class TelemetryStore
{
    private readonly object _lock = new();
    private readonly List<TelemetryPacket<double>> _doubles = new();
    private readonly List<TelemetryPacket<int>> _integers = new();
    private readonly List<TelemetryPacket<bool>> _booleans = new();
    public List<TelemetryPacket<double>> DoubleTelemetry { get { lock (_lock) return _doubles.ToList(); } }

    public List<TelemetryPacket<int>> IntegerTelemetry { get { lock (_lock) return _integers.ToList(); } }

    public List<TelemetryPacket<bool>> BooleanTelemetry { get { lock (_lock) return _booleans.ToList(); } }

    public void Add(TelemetryPacket<double> packet)
    {
        lock (_lock) _doubles.Add(packet);
    }

    public void Add(TelemetryPacket<int> packet)
    {
        lock (_lock) _integers.Add(packet);
    }

    public void Add(TelemetryPacket<bool> packet)
    {
        lock (_lock) _booleans.Add(packet);
    }
}
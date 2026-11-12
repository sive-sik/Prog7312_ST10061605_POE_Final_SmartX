using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class SensorStore
{
    private readonly List<SensorDevice> _sensors = new();
    private readonly object _lock = new();

    public IReadOnlyList<SensorDevice> GetAll()
    {
        lock (_lock) { return _sensors.ToList(); }
    }

    public SensorDevice? GetById(Guid id)
    {
        lock (_lock) { return _sensors.FirstOrDefault(sensor => sensor.Id == id); }
    }

    public SensorDevice? GetByMacAddress(string macAddress)
    {
        lock (_lock) { return _sensors.FirstOrDefault(sensor =>
            sensor.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase)); }
    }

    public SensorDevice Add(SensorDevice sensor)
    {
        lock (_lock)
        {
            if (_sensors.Any(item => item.MacAddress.Equals(sensor.MacAddress, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A sensor with this MAC address is already registered.");
            _sensors.Add(sensor);
        }
        return sensor;
    }
}
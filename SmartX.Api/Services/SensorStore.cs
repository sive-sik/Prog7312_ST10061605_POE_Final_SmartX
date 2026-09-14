using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class SensorStore
{
    private readonly List<SensorDevice> _sensors = new();

    public IReadOnlyList<SensorDevice> GetAll()
    {
        return _sensors;
    }

    public SensorDevice? GetById(Guid id)
    {
        return _sensors.FirstOrDefault(sensor => sensor.Id == id);
    }

    public SensorDevice? GetByMacAddress(string macAddress)
    {
        return _sensors.FirstOrDefault(sensor =>
            sensor.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
    }

    public SensorDevice Add(SensorDevice sensor)
    {
        _sensors.Add(sensor);
        return sensor;
    }
}
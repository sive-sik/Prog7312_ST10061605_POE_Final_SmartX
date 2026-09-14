using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.Models;
using System.Runtime.InteropServices;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorStore _sensorStore;

    public SensorsController(SensorStore sensorStore)
    {
        _sensorStore = sensorStore;
    }

    [HttpGet]
    public ActionResult<IEnumerable<SensorDevice>> GetAll()
    {
        return Ok(_sensorStore.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<SensorDevice> GetById(Guid id)
    {
        SensorDevice? sensor = _sensorStore.GetById(id);

        if (sensor == null)
        {
            return NotFound();
        }

        return Ok(sensor);
    }

    [HttpPost]
    public ActionResult<SensorDevice> RegisterSensor(SensorDevice sensor)
    {
        if (string.IsNullOrWhiteSpace(sensor.MacAddress))
        {
            return BadRequest("MAC address is required.");
        }

        if (string.IsNullOrWhiteSpace(sensor.Name))
        {
            return BadRequest("Sensor name is required.");
        }

        if (string.IsNullOrWhiteSpace(sensor.DeploymentLocation))
        {
            return BadRequest("Deployment location is required.");
        }

        if (string.IsNullOrWhiteSpace(sensor.Category))
        {
            return BadRequest("Sensor category is required.");
        }

        SensorDevice? existing =
            _sensorStore.GetByMacAddress(sensor.MacAddress);

        if (existing != null)
        {
            return Conflict(
                $"A sensor with MAC address {sensor.MacAddress} is already registered.");
        }

        sensor.Id = Guid.NewGuid();
        sensor.RegisteredAt = DateTime.UtcNow;
        sensor.Status = "Online";

        _sensorStore.Add(sensor);

        return CreatedAtAction(
            nameof(GetById),
            new { id = sensor.Id },
            sensor);
    }
}
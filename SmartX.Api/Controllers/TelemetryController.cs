using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryStore _telemetryStore;
    private readonly SensorStore _sensorStore;

    public TelemetryController(
        TelemetryStore telemetryStore,
        SensorStore sensorStore)
    {
        _telemetryStore = telemetryStore;
        _sensorStore = sensorStore;
    }

    [HttpPost("double")]
    public IActionResult AddDoubleTelemetry(
        TelemetryPacket<double> packet)
    {
        IActionResult? validationResult = ValidateDevice(packet.DeviceId);

        if (validationResult != null)
        {
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(packet.Metric)) { return BadRequest("Metric is required."); }
        if (!double.IsFinite(packet.Value)) { return BadRequest("Reading must be finite."); }
        if (packet.Metric.Equals("Soil Moisture", StringComparison.OrdinalIgnoreCase) && (packet.Value < 0 || packet.Value > 100)) { return BadRequest("Soil moisture must be between 0 and 100."); }
        packet.Timestamp = DateTime.UtcNow;

        _telemetryStore.Add(packet);

        return Ok(packet);
    }

    [HttpPost("integer")]
    public IActionResult AddIntegerTelemetry(
        TelemetryPacket<int> packet)
    {
        IActionResult? validationResult = ValidateDevice(packet.DeviceId);

        if (validationResult != null)
        {
            return validationResult;
        }

        packet.Timestamp = DateTime.UtcNow;

        _telemetryStore.Add(packet);

        return Ok(packet);
    }

    [HttpPost("boolean")]
    public IActionResult AddBooleanTelemetry(
        TelemetryPacket<bool> packet)
    {
        IActionResult? validationResult = ValidateDevice(packet.DeviceId);

        if (validationResult != null)
        {
            return validationResult;
        }

        packet.Timestamp = DateTime.UtcNow;

        _telemetryStore.Add(packet);

        return Ok(packet);
    }

    [HttpGet]
    public IActionResult GetTelemetry()
    {
        return Ok(new
        {
            DoubleTelemetry = _telemetryStore.DoubleTelemetry,
            IntegerTelemetry = _telemetryStore.IntegerTelemetry,
            BooleanTelemetry = _telemetryStore.BooleanTelemetry
        });
    }

    private IActionResult? ValidateDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out Guid id))
        {
            return BadRequest("DeviceId must contain a valid sensor ID.");
        }

        if (_sensorStore.GetById(id) == null)
        {
            return NotFound("The specified sensor is not registered.");
        }

        return null;
    }
}
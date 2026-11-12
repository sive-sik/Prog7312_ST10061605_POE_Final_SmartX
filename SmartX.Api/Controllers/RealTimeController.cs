using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/realtime")]
public class RealTimeController : ControllerBase
{
    private readonly RealTimeProcessingStore _processingStore;
    private readonly SensorStore _sensorStore;

    public RealTimeController(RealTimeProcessingStore processingStore, SensorStore sensorStore)
    {
        _processingStore = processingStore;
        _sensorStore = sensorStore;
    }

    private void SynchroniseDevices()
    {
        _processingStore.SynchroniseDevices(_sensorStore.GetAll());
    }

    [HttpGet("stats")]
    public IActionResult GetStatistics()
    {
        SynchroniseDevices();
        return Ok(_processingStore.GetStatistics());
    }

    [HttpGet("devices")]
    public IActionResult GetDevices(string? search = null, string? category = null, bool alertOnly = false)
    {
        SynchroniseDevices();
        return Ok(_processingStore.SearchDevices(search, category, alertOnly));
    }

    [HttpGet("history")]
    public IActionResult GetHistory(int? count = null)
    {
        if (count < 1) { return BadRequest("Count must be greater than zero."); }
        return Ok(count.HasValue
            ? _processingStore.GetRecentHistory(count.Value)
            : _processingStore.GetHistory());
    }

    [HttpGet("alerts")]
    public IActionResult GetAlerts()
    {
        SynchroniseDevices();
        return Ok(_processingStore.GetActiveAlertSensors());
    }

    [HttpGet("suggestions")]
    public IActionResult GetSuggestions()
    {
        SynchroniseDevices();
        return Ok(_processingStore.GetSuggestedActions());
    }

    [HttpPost("process-next")]
    public IActionResult ProcessNext()
    {
        SynchroniseDevices();
        StreamTelemetryItem? item = _processingStore.ProcessNext();
        return item == null ? NoContent() : Ok(item);
    }

    [HttpPost("commands")]
    public IActionResult IssueCommand(CommandRequest request)
    {
        SynchroniseDevices();
        if (_processingStore.GetDevice(request.SensorId) == null)
        {
            return NotFound("The specified sensor is not registered.");
        }
        if (string.IsNullOrWhiteSpace(request.Command)) { return BadRequest("Command is required."); }
        string command = request.Command.Trim();
        string[] allowed = { "Activate Water Pump", "Disable Water Pump", "Enable Smart Relay", "Disable Smart Relay", "Toggle Actuator", "Restart Device", "Enable Cooling" };
        if (!allowed.Contains(command, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Unsupported command.");
        return Ok(_processingStore.IssueCommand(request.SensorId, command));
    }

    [HttpPost("commands/undo")]
    public IActionResult UndoLastCommand()
    {
        SynchroniseDevices();
        DeviceCommand? command = _processingStore.UndoLastCommand();
        return command == null ? NotFound("There are no commands to undo.") : Ok(command);
    }

    [HttpGet("commands/history")]
    public IActionResult GetCommandHistory()
    {
        return Ok(_processingStore.GetCommandHistory());
    }

    [HttpPost("telemetry")]
    public IActionResult EnqueueTelemetry(StreamTelemetryItem request)
    {
        SynchroniseDevices();
        SensorDevice? device = _processingStore.GetDevice(request.SensorId);
        if (device == null) { return NotFound("The specified sensor is not registered."); }
        if (string.IsNullOrWhiteSpace(request.Metric)) { return BadRequest("Metric is required."); }
        if (string.IsNullOrWhiteSpace(request.Value)) { return BadRequest("Value is required."); }

        string metric = request.Metric.Trim();
        string value = request.Value.Trim();
        if (metric.Equals("Soil Moisture", StringComparison.OrdinalIgnoreCase)
            || metric.Equals("Temperature", StringComparison.OrdinalIgnoreCase)
            || metric.Equals("Power", StringComparison.OrdinalIgnoreCase))
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric) || !double.IsFinite(numeric))
                return BadRequest("A finite numeric value is required for this metric.");
            if (metric.Equals("Soil Moisture", StringComparison.OrdinalIgnoreCase) && (numeric < 0 || numeric > 100))
                return BadRequest("Soil moisture must be between 0 and 100.");
            if (metric.Equals("Power", StringComparison.OrdinalIgnoreCase) && numeric < 0)
                return BadRequest("Power cannot be negative.");
            if (metric.Equals("Temperature", StringComparison.OrdinalIgnoreCase) && (numeric < -100 || numeric > 200))
                return BadRequest("Temperature is outside the supported range.");
        }
        StreamTelemetryItem item = CreateTelemetry(device, metric, value, request.Unit);
        _processingStore.EnqueueTelemetry(item);
        return Ok(item);
    }

    private static StreamTelemetryItem CreateTelemetry(SensorDevice device, string metric, string value, string unit)
    {
        StreamTelemetryItem item = new()
        {
            SensorId = device.Id,
            DeviceName = device.Name,
            MacAddress = device.MacAddress,
            Category = device.Category,
            Metric = metric,
            Value = value,
            Unit = unit
        };

        // Invalid numeric readings stay normal rather than causing a parsing exception.
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double reading)
            && double.IsFinite(reading))
        {
            if (metric.Equals("Soil Moisture", StringComparison.OrdinalIgnoreCase))
            {
                item.AlertState = reading < 20 ? "Critical" : reading < 30 ? "Warning" : "Normal";
            }
            else if (metric.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            {
                item.AlertState = reading > 40 ? "Critical" : reading > 35 ? "Warning" : "Normal";
            }
            else if (metric.Equals("Power", StringComparison.OrdinalIgnoreCase))
            {
                item.AlertState = reading > 4500 ? "Critical" : reading > 3000 ? "Warning" : "Normal";
            }
        }

        item.IsCritical = item.AlertState == "Critical";
        return item;
    }

    [HttpPost("seed")]
    public IActionResult Seed()
    {
        SynchroniseDevices();
        IReadOnlyList<SensorDevice> devices = _processingStore.GetDevices();
        if (devices.Count == 0)
        {
            return BadRequest("Sensors must first be registered in Sensor Data.");
        }

        SensorDevice moistureDevice = devices[0];
        SensorDevice powerDevice = devices.Count > 1 ? devices[1] : devices[0];
        SensorDevice temperatureDevice = devices.Count > 2 ? devices[2] : devices[0];
        List<StreamTelemetryItem> packets = new()
        {
            CreateTelemetry(moistureDevice, "Soil Moisture", "45", "%"),
            CreateTelemetry(moistureDevice, "Soil Moisture", "12", "%"),
            CreateTelemetry(powerDevice, "Power", "3500", "W"),
            CreateTelemetry(temperatureDevice, "Temperature", "25", "°C")
        };
        if (devices.Count > 1)
        {
            packets.Add(CreateTelemetry(powerDevice, "Power", "5000", "W"));
        }
        foreach (StreamTelemetryItem packet in packets) { _processingStore.EnqueueTelemetry(packet); }
        return Ok(new { Message = "Demo telemetry queued. Use process-next to process it.", Packets = packets });
    }

    public class CommandRequest
    {
        public Guid SensorId { get; set; }
        public string Command { get; set; } = string.Empty;
    }
}

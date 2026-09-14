using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SensorStore _sensorStore;
    private readonly TelemetryStore _telemetryStore;

    public HealthController(
        SensorStore sensorStore,
        TelemetryStore telemetryStore)
    {
        _sensorStore = sensorStore;
        _telemetryStore = telemetryStore;
    }

    [HttpGet]
    public ActionResult<IEnumerable<DeviceHealthStatus>> GetHealth()
    {
        List<DeviceHealthStatus> results = new();

        foreach (SensorDevice sensor in _sensorStore.GetAll())
        {
            DeviceHealthStatus health = BuildHealthStatus(sensor);
            results.Add(health);
        }

        return Ok(results);
    }

    private DeviceHealthStatus BuildHealthStatus(SensorDevice sensor)
    {
        string deviceId = sensor.Id.ToString();

        TelemetryPacket<double>? latestDouble =
            _telemetryStore.DoubleTelemetry
                .Where(x => x.DeviceId == deviceId)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();

        TelemetryPacket<int>? latestInteger =
            _telemetryStore.IntegerTelemetry
                .Where(x => x.DeviceId == deviceId)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();

        TelemetryPacket<bool>? latestBoolean =
            _telemetryStore.BooleanTelemetry
                .Where(x => x.DeviceId == deviceId)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();

        DateTime? latestTimestamp = new DateTime?[]
        {
            latestDouble?.Timestamp,
            latestInteger?.Timestamp,
            latestBoolean?.Timestamp
        }
        .Where(x => x.HasValue)
        .Select(x => x!.Value)
        .OrderByDescending(x => x)
        .FirstOrDefault();

        if (latestTimestamp == default)
        {
            return new DeviceHealthStatus
            {
                SensorId = sensor.Id,
                DeviceName = sensor.Name,
                MacAddress = sensor.MacAddress,
                Location = sensor.DeploymentLocation,
                Metric = "No telemetry",
                LatestReading = "No data received",
                LastContact = null,
                Status = "Critical",
                Recommendation =
                    "No telemetry has been received. Check device power, network connectivity and gateway registration."
            };
        }

        if (DateTime.UtcNow - latestTimestamp.Value > TimeSpan.FromMinutes(2))
        {
            return new DeviceHealthStatus
            {
                SensorId = sensor.Id,
                DeviceName = sensor.Name,
                MacAddress = sensor.MacAddress,
                Location = sensor.DeploymentLocation,
                Metric = "Connectivity",
                LatestReading = "Sensor timeout",
                LastContact = latestTimestamp,
                Status = "Critical",
                Recommendation =
                    "The sensor has not reported recently. Check ESP32 power, Wi-Fi connectivity and gateway reachability."
            };
        }

        if (latestDouble != null &&
            latestDouble.Timestamp == latestTimestamp)
        {
            return EvaluateDoubleTelemetry(sensor, latestDouble);
        }

        if (latestInteger != null &&
            latestInteger.Timestamp == latestTimestamp)
        {
            return EvaluateIntegerTelemetry(sensor, latestInteger);
        }

        return new DeviceHealthStatus
        {
            SensorId = sensor.Id,
            DeviceName = sensor.Name,
            MacAddress = sensor.MacAddress,
            Location = sensor.DeploymentLocation,
            Metric = latestBoolean?.Metric ?? "Actuator State",
            LatestReading =
                latestBoolean?.Value == true ? "ON" : "OFF",
            LastContact = latestBoolean?.Timestamp,
            Status = "Healthy",
            Recommendation = "Device is reporting normally."
        };
    }

    private DeviceHealthStatus EvaluateDoubleTelemetry(
        SensorDevice sensor,
        TelemetryPacket<double> packet)
    {
        string status = "Healthy";
        string recommendation = "Reading is within the expected range.";

        if (packet.Metric.Contains(
            "Moisture",
            StringComparison.OrdinalIgnoreCase))
        {
            if (packet.Value < 20)
            {
                status = "Critical";
                recommendation =
                    "Soil moisture is critically low. Inspect irrigation lines and consider activating the appropriate pump or valve.";
            }
            else if (packet.Value < 30)
            {
                status = "Warning";
                recommendation =
                    "Soil moisture is below the preferred range. Monitor the node and inspect irrigation if the value continues falling.";
            }
        }

        if (packet.Metric.Contains(
            "Temperature",
            StringComparison.OrdinalIgnoreCase))
        {
            if (packet.Value > 40)
            {
                status = "Critical";
                recommendation =
                    "Temperature is critically high. Inspect ventilation, cooling equipment and nearby environmental sensors.";
            }
            else if (packet.Value > 35)
            {
                status = "Warning";
                recommendation =
                    "Temperature is above the expected range. Monitor ventilation and verify the sensor reading.";
            }
        }

        return new DeviceHealthStatus
        {
            SensorId = sensor.Id,
            DeviceName = sensor.Name,
            MacAddress = sensor.MacAddress,
            Location = sensor.DeploymentLocation,
            Metric = packet.Metric,
            LatestReading = $"{packet.Value} {packet.Unit}",
            LastContact = packet.Timestamp,
            Status = status,
            Recommendation = recommendation
        };
    }

    private DeviceHealthStatus EvaluateIntegerTelemetry(
        SensorDevice sensor,
        TelemetryPacket<int> packet)
    {
        string status = "Healthy";
        string recommendation = "Power usage is within the expected range.";

        if (packet.Metric.Contains(
            "Power",
            StringComparison.OrdinalIgnoreCase))
        {
            if (packet.Value > 4500)
            {
                status = "Critical";
                recommendation =
                    "Power consumption is critically high. Inspect connected pumps, relays and other high-load equipment.";
            }
            else if (packet.Value > 3000)
            {
                status = "Warning";
                recommendation =
                    "Power usage is elevated. Monitor the smart meter and inspect recently activated equipment.";
            }
        }

        return new DeviceHealthStatus
        {
            SensorId = sensor.Id,
            DeviceName = sensor.Name,
            MacAddress = sensor.MacAddress,
            Location = sensor.DeploymentLocation,
            Metric = packet.Metric,
            LatestReading = $"{packet.Value} {packet.Unit}",
            LastContact = packet.Timestamp,
            Status = status,
            Recommendation = recommendation
        };
    }
}
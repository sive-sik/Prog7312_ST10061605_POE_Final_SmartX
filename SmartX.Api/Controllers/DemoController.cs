using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.DataStructures;
using SmartX.Core.Models;
using SmartX.Core.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly SensorStore _sensorStore;
    private readonly TelemetryStore _telemetryStore;

    public DemoController(
        SensorStore sensorStore,
        TelemetryStore telemetryStore)
    {
        _sensorStore = sensorStore;
        _telemetryStore = telemetryStore;
    }

    // Demonstrates operator overloading
    [HttpGet("operator-overloading")]
    public IActionResult OperatorOverloading()
    {
        PowerReading meterA = new()
        {
            DeviceId = "ESP32-METER-A",
            Watts = 1200
        };

        PowerReading meterB = new()
        {
            DeviceId = "ESP32-METER-B",
            Watts = 850
        };

        PowerReading total = meterA + meterB;
        PowerReading difference = meterA - meterB;

        return Ok(new
        {
            MeterA = meterA.Watts,
            MeterB = meterB.Watts,
            AggregatedPower = total.Watts,
            Difference = difference.Watts
        });
    }

    // Demonstrates jagged arrays -> List<T>
    [HttpGet("telemetry-batches")]
    public IActionResult TelemetryBatches()
    {
        double[][] rawBatches =
        {
            new double[] { 22.4, 22.7, 23.1 },
            new double[] { 45.2, 47.8 },
            new double[] { 18.4, 19.1, 20.2, 21.0 }
        };

        TelemetryBatchStore store = new(rawBatches);

        store.ProcessBatches();

        return Ok(new
        {
            RawBatches = store.GetRawBatches(),
            ProcessedTelemetry = store.ProcessedTelemetry,
            ProcessedCount = store.ProcessedTelemetry.Count
        });
    }

    // Demonstrates recursion
    [HttpGet("recursive-validation")]
    public IActionResult RecursiveValidation()
    {
        DeploymentNode deployment = new()
        {
            Name = "Facility A",
            Type = "Facility",
            IsConfigured = true,
            Children =
            {
                new DeploymentNode
                {
                    Name = "Zone 1",
                    Type = "Zone",
                    IsConfigured = true,
                    Children =
                    {
                        new DeploymentNode
                        {
                            Name = "Sub-Zone B",
                            Type = "Sub-Zone",
                            IsConfigured = true,
                            Children =
                            {
                                new DeploymentNode
                                {
                                    Name = "ESP32 Node 14",
                                    Type = "Node",
                                    IsConfigured = true
                                }
                            }
                        }
                    }
                }
            }
        };

        DeploymentValidator validator = new();

        bool valid = validator.ValidateNode(deployment);

        return Ok(new
        {
            Deployment = "Facility A > Zone 1 > Sub-Zone B > ESP32 Node 14",
            IsValid = valid
        });
    }

    // Demonstrates TelemetryPacket<T>
    [HttpGet("generics")]
    public IActionResult Generics()
    {
        TelemetryPacket<double> temperature = new()
        {
            DeviceId = "ESP32-ENV-01",
            Metric = "Temperature",
            Value = 24.6,
            Unit = "°C"
        };

        TelemetryPacket<int> power = new()
        {
            DeviceId = "ESP32-METER-02",
            Metric = "Power Consumption",
            Value = 1380,
            Unit = "W"
        };

        TelemetryPacket<bool> valve = new()
        {
            DeviceId = "ESP32-VALVE-03",
            Metric = "Valve State",
            Value = true,
            Unit = "Open/Closed"
        };

        return Ok(new
        {
            DoubleTelemetry = temperature,
            IntegerTelemetry = power,
            BooleanTelemetry = valve
        });
    }

    // Seed realistic IoT data
    [HttpPost("seed")]
    public IActionResult SeedDemoData()
    {
        if (_sensorStore.GetAll().Count > 0)
        {
            return Ok(new
            {
                Message = "Demo sensors already exist."
            });
        }

        SensorDevice environmental = new()
        {
            MacAddress = "AA:BB:CC:DD:EE:21",
            Name = "ESP32-HYDRO-21",
            DeploymentLocation =
                "Facility A / Greenhouse 1 / Zone A",
            Category = "Environmental",
            Status = "Online"
        };

        SensorDevice power = new()
        {
            MacAddress = "AA:BB:CC:DD:EE:22",
            Name = "ESP32-POWER-22",
            DeploymentLocation =
                "Facility A / Utility Room",
            Category = "Power Consumption",
            Status = "Online"
        };

        SensorDevice actuator = new()
        {
            MacAddress = "AA:BB:CC:DD:EE:23",
            Name = "ESP32-VALVE-23",
            DeploymentLocation =
                "Facility A / Greenhouse 1 / Irrigation Line",
            Category = "Actuator",
            Status = "Online"
        };

        _sensorStore.Add(environmental);
        _sensorStore.Add(power);
        _sensorStore.Add(actuator);

        _telemetryStore.Add(
            new TelemetryPacket<double>
            {
                DeviceId = environmental.Id.ToString(),
                Metric = "Soil Moisture",
                Value = 14.2,
                Unit = "%",
                Timestamp = DateTime.UtcNow
            });

        _telemetryStore.Add(
            new TelemetryPacket<int>
            {
                DeviceId = power.Id.ToString(),
                Metric = "Power Consumption",
                Value = 4850,
                Unit = "W",
                Timestamp = DateTime.UtcNow
            });

        _telemetryStore.Add(
            new TelemetryPacket<bool>
            {
                DeviceId = actuator.Id.ToString(),
                Metric = "Valve State",
                Value = true,
                Unit = "Open/Closed",
                Timestamp = DateTime.UtcNow
            });

        return Ok(new
        {
            Message = "SmartX demo data seeded successfully.",
            SensorsCreated = 3
        });
    }
}
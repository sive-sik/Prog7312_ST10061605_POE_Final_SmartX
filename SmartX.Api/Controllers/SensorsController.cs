using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using SmartX.Api.Services;
using SmartX.Core.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorStore _sensorStore;
    private readonly AttachmentStore _attachmentStore;

    public SensorsController(
        SensorStore sensorStore,
        AttachmentStore attachmentStore)
    {
        _sensorStore = sensorStore;
        _attachmentStore = attachmentStore;
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
            return NotFound("The specified sensor does not exist.");
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

        sensor.MacAddress = sensor.MacAddress.Trim().ToUpperInvariant();
        sensor.Category = sensor.Category.Trim();
        if (!Regex.IsMatch(sensor.MacAddress, @"\A(?:[0-9A-F]{2}:){5}[0-9A-F]{2}\z"))
        {
            return BadRequest("MAC address must use AA:BB:CC:DD:EE:FF format.");
        }
        if (!new[] { "Environmental", "Power Consumption", "Actuator" }.Contains(sensor.Category, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Category must be Environmental, Power Consumption or Actuator.");
        }
        sensor.Name = sensor.Name.Trim();
        sensor.DeploymentLocation = sensor.DeploymentLocation.Trim();
        if (sensor.Name.Length > 100 || sensor.DeploymentLocation.Length > 200)
        {
            return BadRequest("Name or deployment location is too long.");
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

        try { _sensorStore.Add(sensor); }
        catch (InvalidOperationException) { return Conflict("A sensor with this MAC address is already registered."); }

        return CreatedAtAction(
            nameof(GetById),
            new { id = sensor.Id },
            sensor);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(
        Guid id,
        IFormFile file)
    {
        SensorDevice? sensor = _sensorStore.GetById(id);

        if (sensor == null)
        {
            return NotFound("The specified sensor does not exist.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("Please select a valid file.");
        }

        try
        {
            string storedFileName =
                await _attachmentStore.SaveFileAsync(id, file);

            return Ok(new
            {
                SensorId = id,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                Size = file.Length,
                Message = "Attachment uploaded successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
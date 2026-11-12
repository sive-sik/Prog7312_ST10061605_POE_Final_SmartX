using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/mesh")]
public class MeshController : ControllerBase
{
    private readonly MeshNetworkStore mesh;
    private readonly SensorStore sensors;

    public MeshController(MeshNetworkStore mesh, SensorStore sensors)
    {
        this.mesh = mesh;
        this.sensors = sensors;
    }

    private void Synchronise() => mesh.SynchroniseDevices(sensors.GetAll());

    [HttpGet("topology")]
    public IActionResult GetTopology()
    {
        Synchronise();
        return Ok(mesh.GetTopology());
    }

    [HttpGet("route")]
    public IActionResult GetRoute([FromQuery] Guid fromId, [FromQuery] Guid toId)
    {
        Synchronise();
        return Ok(mesh.GetRoute(fromId, toId));
    }

    [HttpGet("connectivity")]
    public IActionResult GetConnectivity([FromQuery] Guid fromId)
    {
        Synchronise();
        return Ok(mesh.GetConnectivity(fromId));
    }

    [HttpPost("links")]
    public IActionResult AddLink([FromBody] MeshLinkRequest request)
    {
        Synchronise();
        try
        {
            mesh.AddLink(request.FromId, request.ToId, request.Weight);
            return Ok(mesh.GetTopology());
        }
        catch (ArgumentException error)
        {
            return BadRequest(error.Message);
        }
    }

    [HttpPost("devices/{deviceId:guid}/status")]
    public IActionResult SetStatus(Guid deviceId, [FromBody] MeshStatusRequest request)
    {
        Synchronise();
        try
        {
            mesh.SetOffline(deviceId, request.Offline);
            return Ok(mesh.GetTopology());
        }
        catch (ArgumentException error)
        {
            return BadRequest(error.Message);
        }
    }

    public class MeshLinkRequest
    {
        public Guid FromId { get; set; }
        public Guid ToId { get; set; }
        public double Weight { get; set; }
    }

    public class MeshStatusRequest
    {
        public bool Offline { get; set; }
    }
}

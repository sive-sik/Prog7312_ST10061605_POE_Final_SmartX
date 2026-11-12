using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Core.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/telemetry-history")]
public class TelemetryHistoryController : ControllerBase
{
    private readonly RealTimeProcessingStore _store;

    public TelemetryHistoryController(RealTimeProcessingStore store)
    {
        _store = store;
    }

    [HttpGet("range")]
    public ActionResult<IReadOnlyList<StreamTelemetryItem>> GetRange(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] Guid? sensorId = null)
    {
        if (start == default || end == default || start > end)
        {
            return BadRequest("Provide a valid start and end timestamp, with start before end.");
        }

        return Ok(_store.GetHistoricalRange(start, end, sensorId));
    }
}

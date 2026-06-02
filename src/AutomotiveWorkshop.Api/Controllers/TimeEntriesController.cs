using AutomotiveWorkshop.Application.DTOs.TimeTracking;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Shop)]
public class TimeEntriesController : ControllerBase
{
    private readonly ITimeTrackingService _timeTracking;

    public TimeEntriesController(ITimeTrackingService timeTracking) => _timeTracking = timeTracking;

    [HttpGet("api/v1/work-orders/{workOrderId:guid}/time-entries")]
    public async Task<IActionResult> List(Guid workOrderId, CancellationToken ct)
        => Ok(await _timeTracking.GetForWorkOrderAsync(workOrderId, ct));

    [HttpPost("api/v1/work-orders/{workOrderId:guid}/time-entries/clock-in")]
    public async Task<IActionResult> ClockIn(Guid workOrderId, [FromBody] ClockInRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var entry = await _timeTracking.ClockInAsync(workOrderId, request, userId, ct);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPatch("api/v1/time-entries/{id:guid}/clock-out")]
    public async Task<IActionResult> ClockOut(Guid id, [FromBody] ClockOutRequest request, CancellationToken ct)
    {
        var entry = await _timeTracking.ClockOutAsync(id, request, ct);
        return entry is null ? NotFound() : Ok(entry);
    }
}

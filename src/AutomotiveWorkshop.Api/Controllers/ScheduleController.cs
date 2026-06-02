using AutomotiveWorkshop.Application.DTOs.Schedule;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/schedule")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    public ScheduleController(IScheduleService scheduleService) => _scheduleService = scheduleService;

    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var events = await _scheduleService.GetEventsAsync(from, to, ct);
        return Ok(events);
    }

    [HttpPatch("work-orders/{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> UpdateWorkOrderSchedule(
        Guid id,
        [FromBody] UpdateWorkOrderScheduleRequest request,
        CancellationToken ct)
    {
        var evt = await _scheduleService.UpdateScheduleAsync(id, request, ct);
        return evt is null ? NotFound() : Ok(evt);
    }

    [HttpPatch("work-orders/{id:guid}/assignment")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> AssignTechnician(
        Guid id,
        [FromBody] AssignWorkOrderRequest request,
        CancellationToken ct)
    {
        var evt = await _scheduleService.AssignTechnicianAsync(id, request, ct);
        return evt is null ? NotFound() : Ok(evt);
    }
}

using AutomotiveWorkshop.Application.DTOs.WorkOrders;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersController(IWorkOrderService workOrderService) => _workOrderService = workOrderService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] WorkOrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _workOrderService.GetAllAsync(status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var workOrder = await _workOrderService.GetByIdAsync(id, ct);
        return workOrder is null ? NotFound() : Ok(workOrder);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request, CancellationToken ct)
    {
        var workOrder = await _workOrderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = workOrder.Id }, workOrder);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWorkOrderStatusRequest request, CancellationToken ct)
    {
        var workOrder = await _workOrderService.UpdateStatusAsync(id, request, ct);
        return workOrder is null ? NotFound() : Ok(workOrder);
    }
}

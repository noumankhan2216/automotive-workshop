using AutomotiveWorkshop.Application.DTOs.Estimates;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/estimates")]
[Authorize]
public class EstimatesController : ControllerBase
{
    private readonly IEstimateService _estimateService;
    private readonly IDocumentService _documentService;

    public EstimatesController(IEstimateService estimateService, IDocumentService documentService)
    {
        _estimateService = estimateService;
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EstimateStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _estimateService.GetAllAsync(status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var estimate = await _estimateService.GetByIdAsync(id, ct);
        return estimate is null ? NotFound() : Ok(estimate);
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Create([FromBody] CreateEstimateRequest request, CancellationToken ct)
    {
        var estimate = await _estimateService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = estimate.Id }, estimate);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEstimateRequest request, CancellationToken ct)
    {
        try
        {
            var estimate = await _estimateService.UpdateAsync(id, request, ct);
            return estimate is null ? NotFound() : Ok(estimate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEstimateStatusRequest request, CancellationToken ct)
    {
        try
        {
            var estimate = await _estimateService.UpdateStatusAsync(id, request, ct);
            return estimate is null ? NotFound() : Ok(estimate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/convert")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Convert(Guid id, CancellationToken ct)
    {
        try
        {
            var workOrder = await _estimateService.ConvertToWorkOrderAsync(id, ct);
            return workOrder is null ? NotFound() : Ok(workOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var bytes = await _documentService.GenerateEstimatePdfAsync(id, ct);
        return bytes is null ? NotFound() : File(bytes, "application/pdf", $"estimate-{id}.pdf");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _estimateService.DeleteAsync(id, ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

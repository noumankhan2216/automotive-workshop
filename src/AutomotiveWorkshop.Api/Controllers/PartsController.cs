using AutomotiveWorkshop.Application.DTOs.Parts;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IPartService _partService;

    public PartsController(IPartService partService) => _partService = partService;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _partService.GetAllAsync(search, lowStockOnly, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var part = await _partService.GetByIdAsync(id, ct);
        return part is null ? NotFound() : Ok(part);
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> Transactions(Guid id, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var txs = await _partService.GetTransactionsAsync(id, limit, ct);
        return Ok(txs);
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Create([FromBody] CreatePartRequest request, CancellationToken ct)
    {
        var part = await _partService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartRequest request, CancellationToken ct)
    {
        var part = await _partService.UpdateAsync(id, request, ct);
        return part is null ? NotFound() : Ok(part);
    }

    [HttpPost("{id:guid}/adjust-stock")]
    [Authorize(Roles = Roles.Shop)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustPartStockRequest request, CancellationToken ct)
    {
        var part = await _partService.AdjustStockAsync(id, request, ct);
        return part is null ? NotFound() : Ok(part);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _partService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}

using AutomotiveWorkshop.Application.DTOs.ServiceCatalog;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/service-catalog")]
[Authorize]
public class ServiceCatalogController : ControllerBase
{
    private readonly IServiceCatalogService _service;

    public ServiceCatalogController(IServiceCatalogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(includeInactive, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Create([FromBody] CreateServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = await _service.UpdateAsync(id, request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}

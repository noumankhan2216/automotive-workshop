using AutomotiveWorkshop.Application.DTOs.Vehicles;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService) => _vehicleService = vehicleService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _vehicleService.GetAllAsync(search, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, ct);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var vehicle = await _vehicleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        var vehicle = await _vehicleService.UpdateAsync(id, request, ct);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.FrontOffice)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _vehicleService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}

using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = Roles.FrontOffice)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService) => _reportService = reportService;

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await _reportService.GetSalesReportAsync(from, to, ct));

    [HttpGet("tax")]
    public async Task<IActionResult> Tax([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await _reportService.GetTaxReportAsync(from, to, ct));

    [HttpGet("technician-productivity")]
    public async Task<IActionResult> TechnicianProductivity(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
        => Ok(await _reportService.GetTechnicianProductivityAsync(from, to, ct));
}

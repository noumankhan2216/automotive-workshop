using AutomotiveWorkshop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _dashboardService.GetSummaryAsync(ct);
        return Ok(summary);
    }

    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var chart = await _dashboardService.GetRevenueChartAsync(days, ct);
        return Ok(chart);
    }

    [HttpGet("top-services")]
    public async Task<IActionResult> GetTopServices([FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var services = await _dashboardService.GetTopServicesAsync(limit, ct);
        return Ok(services);
    }
}

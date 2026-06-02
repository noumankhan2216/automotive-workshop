using AutomotiveWorkshop.Application.DTOs.Dashboard;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<RevenueChartPointDto>> GetRevenueChartAsync(int days, CancellationToken ct);
    Task<IReadOnlyList<TopServiceDto>> GetTopServicesAsync(int limit, CancellationToken ct);
}

public class DashboardService : IDashboardService
{
    private readonly DbContext _db;

    public DashboardService(DbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidInvoices = _db.Set<Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid && i.PaidAt != null);

        var revenueToday = await paidInvoices.Where(i => i.PaidAt!.Value.Date == today).SumAsync(i => i.Total, ct);
        var revenueWeek = await paidInvoices.Where(i => i.PaidAt!.Value >= weekStart).SumAsync(i => i.Total, ct);
        var revenueMonth = await paidInvoices.Where(i => i.PaidAt!.Value >= monthStart).SumAsync(i => i.Total, ct);

        var openWorkOrders = await _db.Set<WorkOrder>().AsNoTracking()
            .CountAsync(w => !w.IsDeleted && w.Status != WorkOrderStatus.Completed &&
                             w.Status != WorkOrderStatus.Cancelled &&
                             w.Status != WorkOrderStatus.Paid, ct);

        var completedThisMonth = await _db.Set<WorkOrder>().AsNoTracking()
            .CountAsync(w => !w.IsDeleted && w.CompletedAt >= monthStart, ct);

        var outstanding = await _db.Set<Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue))
            .ToListAsync(ct);

        var lowStockParts = await _db.Set<Part>().AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.IsActive && p.QuantityOnHand <= p.ReorderLevel, ct);

        return new DashboardSummaryDto(
            revenueToday, revenueWeek, revenueMonth,
            openWorkOrders, completedThisMonth,
            outstanding.Count, outstanding.Sum(i => i.Total),
            lowStockParts);
    }

    public async Task<IReadOnlyList<RevenueChartPointDto>> GetRevenueChartAsync(int days, CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date.AddDays(-days + 1);

        var data = await _db.Set<Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid && i.PaidAt >= start)
            .GroupBy(i => i.PaidAt!.Value.Date)
            .Select(g => new RevenueChartPointDto(g.Key.ToString("MMM dd"), g.Sum(i => i.Total)))
            .ToListAsync(ct);

        return data;
    }

    public async Task<IReadOnlyList<TopServiceDto>> GetTopServicesAsync(int limit, CancellationToken ct)
    {
        return await _db.Set<WorkOrderItem>().AsNoTracking()
            .GroupBy(i => i.Description)
            .Select(g => new TopServiceDto(g.Key, g.Count(), g.Sum(i => i.LineTotal)))
            .OrderByDescending(s => s.Revenue)
            .Take(limit)
            .ToListAsync(ct);
    }
}

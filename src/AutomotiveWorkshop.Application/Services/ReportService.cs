using AutomotiveWorkshop.Application.DTOs.Reports;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct);
    Task<TaxReportDto> GetTaxReportAsync(DateTime from, DateTime to, CancellationToken ct);
    Task<TechnicianProductivityReportDto> GetTechnicianProductivityAsync(DateTime from, DateTime to, CancellationToken ct);
}

public class ReportService : IReportService
{
    private readonly DbContext _db;
    private readonly IUserDirectoryService _users;

    public ReportService(DbContext db, IUserDirectoryService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var invoices = await _db.Set<Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.IssuedAt >= from && i.IssuedAt < to)
            .ToListAsync(ct);

        var paid = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
        var gross = invoices.Sum(i => i.Total);
        var tax = invoices.Sum(i => i.TaxAmount);
        var net = paid.Sum(i => i.Total);

        var rows = invoices
            .GroupBy(i => i.IssuedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesReportRowDto(
                g.Key,
                g.Count(),
                g.Sum(i => i.SubTotal),
                g.Sum(i => i.TaxAmount),
                g.Sum(i => i.Total)))
            .ToList();

        return new SalesReportDto(from, to, gross, tax, net, invoices.Count, paid.Count, rows);
    }

    public async Task<TaxReportDto> GetTaxReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var invoices = await _db.Set<Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted &&
                        i.IssuedAt >= from && i.IssuedAt < to &&
                        i.Status != InvoiceStatus.Void)
            .ToListAsync(ct);

        var taxable = invoices.Sum(i => i.SubTotal);
        var tax = invoices.Sum(i => i.TaxAmount);
        var rate = taxable > 0 ? Math.Round(tax / taxable, 4) : 0m;

        var rows = invoices
            .GroupBy(i => i.IssuedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TaxReportRowDto(g.Key, g.Sum(i => i.SubTotal), g.Sum(i => i.TaxAmount)))
            .ToList();

        return new TaxReportDto(from, to, taxable, tax, rate, rows);
    }

    public async Task<TechnicianProductivityReportDto> GetTechnicianProductivityAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var technicians = await _users.GetTechniciansAsync(ct);
        var entries = await _db.Set<TimeEntry>().AsNoTracking()
            .Where(t => t.StartedAt < to && (t.EndedAt == null || t.EndedAt > from))
            .ToListAsync(ct);

        var workOrders = await _db.Set<WorkOrder>().AsNoTracking()
            .Where(w => !w.IsDeleted && w.AssignedToUserId != null)
            .ToListAsync(ct);

        var rows = new List<TechnicianProductivityRowDto>();
        foreach (var tech in technicians)
        {
            var techEntries = entries.Where(e => e.UserId == tech.Id).ToList();
            var hours = techEntries.Sum(e =>
            {
                var start = e.StartedAt < from ? from : e.StartedAt;
                var end = e.EndedAt ?? DateTime.UtcNow;
                if (end > to) end = to;
                return end > start ? (decimal)(end - start).TotalHours : 0m;
            });

            var assigned = workOrders.Count(w =>
                w.AssignedToUserId == tech.Id &&
                w.OpenedAt >= from && w.OpenedAt < to);

            var completed = workOrders.Count(w =>
                w.AssignedToUserId == tech.Id &&
                w.CompletedAt >= from && w.CompletedAt < to);

            var openEntries = techEntries.Count(e => e.EndedAt == null);

            rows.Add(new TechnicianProductivityRowDto(
                tech.Id, tech.FullName, Math.Round(hours, 2), assigned, completed, openEntries));
        }

        return new TechnicianProductivityReportDto(from, to, rows.OrderByDescending(r => r.TotalHours).ToList());
    }
}

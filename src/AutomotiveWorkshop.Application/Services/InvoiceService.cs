using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.Invoices;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetAllAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken ct);
    Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InvoiceDetailDto> CreateFromWorkOrderAsync(CreateInvoiceFromWorkOrderRequest request, CancellationToken ct);
    Task<InvoiceDetailDto?> UpdateStatusAsync(Guid id, UpdateInvoiceStatusRequest request, CancellationToken ct);
}

public class InvoiceService : IInvoiceService
{
    private readonly DbContext _db;
    private readonly INotificationService _notificationService;

    public InvoiceService(DbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<InvoiceDto>> GetAllAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Invoice>().AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync(ct);
        var invoices = await query
            .OrderByDescending(i => i.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = invoices.Select(MapToDto).ToList();
        return new PagedResult<InvoiceDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var invoice = await _db.Set<Invoice>()
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

        return invoice is null ? null : MapToDetailDto(invoice);
    }

    public async Task<InvoiceDetailDto> CreateFromWorkOrderAsync(CreateInvoiceFromWorkOrderRequest request, CancellationToken ct)
    {
        var workOrder = await _db.Set<WorkOrder>()
            .Include(w => w.Items)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.Id == request.WorkOrderId && !w.IsDeleted, ct)
            ?? throw new InvalidOperationException("Work order not found.");

        if (workOrder.Status != WorkOrderStatus.Completed && workOrder.Status != WorkOrderStatus.Invoiced)
            throw new InvalidOperationException("Work order must be completed before invoicing.");

        var existing = await _db.Set<Invoice>()
            .AnyAsync(i => i.WorkOrderId == workOrder.Id && !i.IsDeleted, ct);

        if (existing)
            throw new InvalidOperationException("Invoice already exists for this work order.");

        var settings = await _db.Set<WorkshopSettings>().FirstAsync(ct);
        var subTotal = workOrder.Items.Sum(i => i.LineTotal);
        var taxAmount = Math.Round(subTotal * settings.DefaultTaxRate, 2);

        var count = await _db.Set<Invoice>().CountAsync(ct);
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            CustomerId = workOrder.CustomerId,
            WorkOrderId = workOrder.Id,
            Status = InvoiceStatus.Draft,
            SubTotal = subTotal,
            TaxRate = settings.DefaultTaxRate,
            TaxAmount = taxAmount,
            Total = subTotal + taxAmount,
            DueDate = DateTime.UtcNow.AddDays(14),
            Lines = workOrder.Items.Select(i => new InvoiceLine
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        workOrder.Status = WorkOrderStatus.Invoiced;
        workOrder.UpdatedAt = DateTime.UtcNow;

        _db.Set<Invoice>().Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(invoice.Id, ct))!;
    }

    public async Task<InvoiceDetailDto?> UpdateStatusAsync(Guid id, UpdateInvoiceStatusRequest request, CancellationToken ct)
    {
        var invoice = await _db.Set<Invoice>()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

        if (invoice is null) return null;

        invoice.Status = request.Status;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (request.Status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (request.Status == InvoiceStatus.Sent && !string.IsNullOrWhiteSpace(invoice.Customer.Email))
        {
            await _notificationService.SendEmailAsync(
                invoice.Customer.Email,
                $"Invoice {invoice.InvoiceNumber}",
                $"Your invoice total is {invoice.Total:C}. Due date: {invoice.DueDate:d}.",
                ct);
        }

        return MapToDetailDto(invoice);
    }

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.CustomerId, i.Customer.Name, i.WorkOrderId,
        i.Status, i.SubTotal, i.TaxAmount, i.Total, i.IssuedAt, i.DueDate, i.PaidAt);

    private static InvoiceDetailDto MapToDetailDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.CustomerId, i.Customer.Name, i.WorkOrderId,
        i.Status, i.SubTotal, i.TaxRate, i.TaxAmount, i.Total,
        i.IssuedAt, i.DueDate, i.PaidAt, i.Notes,
        i.Lines.Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());
}

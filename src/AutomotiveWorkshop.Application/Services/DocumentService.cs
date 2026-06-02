using AutomotiveWorkshop.Application.Documents;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IDocumentService
{
    Task<byte[]?> GenerateEstimatePdfAsync(Guid id, CancellationToken ct);
    Task<byte[]?> GenerateWorkOrderPdfAsync(Guid id, CancellationToken ct);
    Task<byte[]?> GenerateInvoicePdfAsync(Guid id, CancellationToken ct);
}

public class DocumentService : IDocumentService
{
    private readonly DbContext _db;
    private readonly IPdfService _pdf;

    public DocumentService(DbContext db, IPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    public async Task<byte[]?> GenerateEstimatePdfAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Set<Estimate>().AsNoTracking()
            .Include(x => x.Customer).Include(x => x.Vehicle).Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (e is null) return null;

        var shop = await GetShopAsync(ct);
        var subTotal = e.Items.Sum(i => i.LineTotal);
        var tax = Math.Round(subTotal * e.TaxRate, 2);

        var model = new DocumentModel(
            "ESTIMATE", e.EstimateNumber, e.Status.ToString(),
            e.CreatedAt, e.ValidUntil, "Valid Until",
            shop.info, ToParty(e.Customer), ToVehicle(e.Vehicle),
            ToLines(e.Items.Select(i => (i.Description, i.Quantity, i.UnitPrice, i.LineTotal))),
            subTotal, e.TaxRate, tax, subTotal + tax, 0m, e.CustomerNotes);

        return _pdf.Render(model);
    }

    public async Task<byte[]?> GenerateWorkOrderPdfAsync(Guid id, CancellationToken ct)
    {
        var w = await _db.Set<WorkOrder>().AsNoTracking()
            .Include(x => x.Customer).Include(x => x.Vehicle).Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (w is null) return null;

        var shop = await GetShopAsync(ct);
        var subTotal = w.Items.Sum(i => i.LineTotal);
        var tax = Math.Round(subTotal * shop.taxRate, 2);

        var model = new DocumentModel(
            "WORK ORDER", w.WorkOrderNumber, w.Status.ToString(),
            w.OpenedAt, w.CompletedAt, "Completed",
            shop.info, ToParty(w.Customer), ToVehicle(w.Vehicle),
            ToLines(w.Items.Select(i => (i.Description, i.Quantity, i.UnitPrice, i.LineTotal))),
            subTotal, shop.taxRate, tax, subTotal + tax, 0m, w.CustomerNotes);

        return _pdf.Render(model);
    }

    public async Task<byte[]?> GenerateInvoicePdfAsync(Guid id, CancellationToken ct)
    {
        var inv = await _db.Set<Invoice>().AsNoTracking()
            .Include(x => x.Customer).Include(x => x.Lines)
            .Include(x => x.WorkOrder)!.ThenInclude(w => w!.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (inv is null) return null;

        var shop = await GetShopAsync(ct);
        var vehicle = inv.WorkOrder?.Vehicle is { } v ? ToVehicle(v) : null;
        var balanceDue = inv.Status == InvoiceStatus.Paid ? 0m : inv.Total;

        var model = new DocumentModel(
            "INVOICE", inv.InvoiceNumber, inv.Status.ToString(),
            inv.IssuedAt, inv.DueDate, "Due Date",
            shop.info, ToParty(inv.Customer), vehicle,
            ToLines(inv.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice, l.LineTotal))),
            inv.SubTotal, inv.TaxRate, inv.TaxAmount, inv.Total, balanceDue, inv.Notes);

        return _pdf.Render(model);
    }

    private async Task<(WorkshopInfo info, decimal taxRate)> GetShopAsync(CancellationToken ct)
    {
        var s = await _db.Set<WorkshopSettings>().AsNoTracking().FirstAsync(ct);
        return (new WorkshopInfo(s.WorkshopName, s.Address, s.Phone, s.Email, s.CurrencyCode), s.DefaultTaxRate);
    }

    private static PartyInfo ToParty(Customer c) => new(c.Name, c.Email, c.Phone, c.Address);

    private static VehicleInfo ToVehicle(Vehicle v) =>
        new($"{v.Year} {v.Make} {v.Model}", v.Vin, v.LicensePlate, v.Mileage);

    private static IReadOnlyList<DocumentLine> ToLines(
        IEnumerable<(string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal)> source) =>
        source.Select((l, i) => new DocumentLine(i + 1, l.Description, l.Quantity, l.UnitPrice, l.LineTotal)).ToList();
}

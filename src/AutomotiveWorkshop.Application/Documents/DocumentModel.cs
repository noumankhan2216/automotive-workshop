namespace AutomotiveWorkshop.Application.Documents;

/// <summary>A unified, render-agnostic representation of a printable shop document
/// (estimate, work order, or invoice) used to produce PDFs.</summary>
public record DocumentModel(
    string DocumentType,
    string DocumentNumber,
    string StatusLabel,
    DateTime IssuedAt,
    DateTime? SecondaryDate,
    string SecondaryDateLabel,
    WorkshopInfo Shop,
    PartyInfo Customer,
    VehicleInfo? Vehicle,
    IReadOnlyList<DocumentLine> Lines,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    decimal BalanceDue,
    string? Notes);

public record WorkshopInfo(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string CurrencyCode);

public record PartyInfo(
    string Name,
    string? Email,
    string? Phone,
    string? Address);

public record VehicleInfo(
    string Description,
    string? Vin,
    string? LicensePlate,
    int? Mileage);

public record DocumentLine(
    int Index,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

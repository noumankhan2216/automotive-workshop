namespace AutomotiveWorkshop.Application.DTOs.Reports;

public record SalesReportDto(
    DateTime From,
    DateTime To,
    decimal GrossSales,
    decimal TaxCollected,
    decimal NetSales,
    int InvoiceCount,
    int PaidInvoiceCount,
    IReadOnlyList<SalesReportRowDto> Rows);

public record SalesReportRowDto(
    DateTime Date,
    int InvoiceCount,
    decimal SubTotal,
    decimal TaxAmount,
    decimal Total);

public record TaxReportDto(
    DateTime From,
    DateTime To,
    decimal TaxableSales,
    decimal TaxCollected,
    decimal EffectiveTaxRate,
    IReadOnlyList<TaxReportRowDto> Rows);

public record TaxReportRowDto(
    DateTime Date,
    decimal TaxableAmount,
    decimal TaxAmount);

public record TechnicianProductivityReportDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<TechnicianProductivityRowDto> Technicians);

public record TechnicianProductivityRowDto(
    string UserId,
    string UserName,
    decimal TotalHours,
    int JobsAssigned,
    int JobsCompleted,
    int OpenTimeEntries);

public record TechnicianUserDto(string Id, string FullName, string Email);

using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Application.DTOs.Invoices;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    Guid? WorkOrderId,
    InvoiceStatus Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal Total,
    DateTime IssuedAt,
    DateTime? DueDate,
    DateTime? PaidAt);

public record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    Guid? WorkOrderId,
    InvoiceStatus Status,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    DateTime IssuedAt,
    DateTime? DueDate,
    DateTime? PaidAt,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines);

public record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record CreateInvoiceFromWorkOrderRequest(Guid WorkOrderId);

public record UpdateInvoiceStatusRequest(InvoiceStatus Status);

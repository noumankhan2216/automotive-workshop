using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Application.DTOs.Estimates;

public record EstimateDto(
    Guid Id,
    string EstimateNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleDescription,
    EstimateStatus Status,
    DateTime CreatedAt,
    DateTime? ValidUntil,
    Guid? ConvertedWorkOrderId,
    decimal TotalAmount);

public record EstimateDetailDto(
    Guid Id,
    string EstimateNumber,
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? CustomerAddress,
    Guid VehicleId,
    string VehicleDescription,
    string? VehicleVin,
    string? VehicleLicensePlate,
    EstimateStatus Status,
    string? CustomerNotes,
    string? InternalNotes,
    DateTime CreatedAt,
    DateTime? ValidUntil,
    DateTime? ApprovedAt,
    Guid? ConvertedWorkOrderId,
    IReadOnlyList<EstimateItemDto> Items,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total);

public record EstimateItemDto(
    Guid Id,
    Guid? ServiceCatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record CreateEstimateRequest(
    Guid CustomerId,
    Guid VehicleId,
    string? CustomerNotes,
    string? InternalNotes,
    DateTime? ValidUntil,
    IReadOnlyList<EstimateItemRequest> Items);

public record UpdateEstimateRequest(
    string? CustomerNotes,
    string? InternalNotes,
    DateTime? ValidUntil,
    IReadOnlyList<EstimateItemRequest> Items);

public record EstimateItemRequest(
    Guid? ServiceCatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record UpdateEstimateStatusRequest(EstimateStatus Status);

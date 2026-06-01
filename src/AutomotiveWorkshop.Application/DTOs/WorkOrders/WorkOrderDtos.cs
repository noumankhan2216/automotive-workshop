using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Application.DTOs.WorkOrders;

public record WorkOrderDto(
    Guid Id,
    string WorkOrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleDescription,
    WorkOrderStatus Status,
    string? AssignedToUserId,
    DateTime OpenedAt,
    DateTime? CompletedAt,
    decimal TotalAmount);

public record WorkOrderDetailDto(
    Guid Id,
    string WorkOrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleDescription,
    WorkOrderStatus Status,
    string? AssignedToUserId,
    string? CustomerNotes,
    string? InternalNotes,
    DateTime OpenedAt,
    DateTime? CompletedAt,
    IReadOnlyList<WorkOrderItemDto> Items,
    decimal TotalAmount);

public record WorkOrderItemDto(
    Guid Id,
    Guid? ServiceCatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record CreateWorkOrderRequest(
    Guid CustomerId,
    Guid VehicleId,
    string? CustomerNotes,
    string? InternalNotes,
    IReadOnlyList<CreateWorkOrderItemRequest> Items);

public record CreateWorkOrderItemRequest(
    Guid? ServiceCatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record UpdateWorkOrderStatusRequest(WorkOrderStatus Status);

using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Application.DTOs.Parts;

public record PartDto(
    Guid Id,
    string Sku,
    string Name,
    string? Category,
    decimal UnitCost,
    decimal UnitPrice,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsActive,
    bool IsLowStock);

public record PartDetailDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string? Category,
    decimal UnitCost,
    decimal UnitPrice,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsActive,
    bool IsLowStock);

public record CreatePartRequest(
    string Sku,
    string Name,
    string? Description,
    string? Category,
    decimal UnitCost,
    decimal UnitPrice,
    decimal QuantityOnHand,
    decimal ReorderLevel);

public record UpdatePartRequest(
    string Sku,
    string Name,
    string? Description,
    string? Category,
    decimal UnitCost,
    decimal UnitPrice,
    decimal ReorderLevel,
    bool IsActive);

public record AdjustPartStockRequest(
    decimal QuantityChange,
    PartStockTransactionType Type,
    Guid? WorkOrderId,
    string? Notes);

public record PartStockTransactionDto(
    Guid Id,
    PartStockTransactionType Type,
    decimal QuantityChange,
    decimal QuantityAfter,
    string? Notes,
    DateTime CreatedAt);

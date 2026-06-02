namespace AutomotiveWorkshop.Application.DTOs.ServiceCatalog;

public record ServiceCatalogItemDto(
    Guid Id,
    string Name,
    string? Description,
    decimal DefaultPrice,
    bool IsActive);

public record CreateServiceCatalogItemRequest(
    string Name,
    string? Description,
    decimal DefaultPrice);

public record UpdateServiceCatalogItemRequest(
    string Name,
    string? Description,
    decimal DefaultPrice,
    bool IsActive);

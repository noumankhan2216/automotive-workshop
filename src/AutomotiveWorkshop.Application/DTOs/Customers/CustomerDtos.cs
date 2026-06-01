namespace AutomotiveWorkshop.Application.DTOs.Customers;

public record CustomerDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    int VehicleCount,
    DateTime CreatedAt);

public record CustomerDetailDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<VehicleSummaryDto> Vehicles);

public record VehicleSummaryDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string? LicensePlate);

public record CreateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);

public record UpdateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);

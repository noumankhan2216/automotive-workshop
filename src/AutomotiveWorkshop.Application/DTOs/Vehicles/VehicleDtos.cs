namespace AutomotiveWorkshop.Application.DTOs.Vehicles;

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Make,
    string Model,
    int Year,
    string? Vin,
    string? LicensePlate,
    int? Mileage,
    string? Color);

public record CreateVehicleRequest(
    Guid CustomerId,
    string Make,
    string Model,
    int Year,
    string? Vin,
    string? LicensePlate,
    int? Mileage,
    string? Color);

public record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string? Vin,
    string? LicensePlate,
    int? Mileage,
    string? Color);

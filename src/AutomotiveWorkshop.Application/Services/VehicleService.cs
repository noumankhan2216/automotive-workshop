using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.Vehicles;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using AutomotiveWorkshop.Domain.Entities;

namespace AutomotiveWorkshop.Application.Services;

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct);
    Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class VehicleService : IVehicleService
{
    private readonly DbContext _db;
    private readonly IValidator<CreateVehicleRequest> _createValidator;
    private readonly IValidator<UpdateVehicleRequest> _updateValidator;

    public VehicleService(
        DbContext db,
        IValidator<CreateVehicleRequest> createValidator,
        IValidator<UpdateVehicleRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<VehicleDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Vehicle>().AsNoTracking()
            .Include(v => v.Customer)
            .Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.Make.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term) ||
                (v.LicensePlate != null && v.LicensePlate.ToLower().Contains(term)) ||
                v.Customer.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VehicleDto(
                v.Id, v.CustomerId, v.Customer.Name, v.Make, v.Model, v.Year,
                v.Vin, v.LicensePlate, v.Mileage, v.Color))
            .ToListAsync(ct);

        return new PagedResult<VehicleDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<Vehicle>().AsNoTracking()
            .Include(v => v.Customer)
            .Where(v => v.Id == id && !v.IsDeleted)
            .Select(v => new VehicleDto(
                v.Id, v.CustomerId, v.Customer.Name, v.Make, v.Model, v.Year,
                v.Vin, v.LicensePlate, v.Mileage, v.Color))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var customerExists = await _db.Set<Customer>()
            .AnyAsync(c => c.Id == request.CustomerId && !c.IsDeleted, ct);

        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        var vehicle = new Vehicle
        {
            CustomerId = request.CustomerId,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Vin = request.Vin?.Trim(),
            LicensePlate = request.LicensePlate?.Trim(),
            Mileage = request.Mileage,
            Color = request.Color?.Trim()
        };

        _db.Set<Vehicle>().Add(vehicle);
        await _db.SaveChangesAsync(ct);

        var customerName = await _db.Set<Customer>()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => c.Name)
            .FirstAsync(ct);

        return new VehicleDto(vehicle.Id, vehicle.CustomerId, customerName, vehicle.Make,
            vehicle.Model, vehicle.Year, vehicle.Vin, vehicle.LicensePlate, vehicle.Mileage, vehicle.Color);
    }

    public async Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var vehicle = await _db.Set<Vehicle>()
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);

        if (vehicle is null) return null;

        vehicle.Make = request.Make.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.Vin = request.Vin?.Trim();
        vehicle.LicensePlate = request.LicensePlate?.Trim();
        vehicle.Mileage = request.Mileage;
        vehicle.Color = request.Color?.Trim();
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new VehicleDto(vehicle.Id, vehicle.CustomerId, vehicle.Customer.Name, vehicle.Make,
            vehicle.Model, vehicle.Year, vehicle.Vin, vehicle.LicensePlate, vehicle.Mileage, vehicle.Color);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var vehicle = await _db.Set<Vehicle>().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
        if (vehicle is null) return false;

        vehicle.IsDeleted = true;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

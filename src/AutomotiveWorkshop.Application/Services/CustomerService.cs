using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.Customers;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<CustomerDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct);
    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class CustomerService : ICustomerService
{
    private readonly DbContext _db;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomerService(
        DbContext db,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<CustomerDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Customer>().AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto(
                c.Id, c.Name, c.Email, c.Phone, c.Address, c.Notes,
                c.Vehicles.Count(v => !v.IsDeleted), c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var customer = await _db.Set<Customer>()
            .AsNoTracking()
            .Include(c => c.Vehicles.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (customer is null) return null;

        return new CustomerDetailDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.Notes,
            customer.CreatedAt,
            customer.Vehicles.Select(v => new VehicleSummaryDto(
                v.Id, v.Make, v.Model, v.Year, v.LicensePlate)).ToList());
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _db.Set<Customer>().Add(customer);
        await _db.SaveChangesAsync(ct);

        return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone,
            customer.Address, customer.Notes, 0, customer.CreatedAt);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var customer = await _db.Set<Customer>()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (customer is null) return null;

        customer.Name = request.Name.Trim();
        customer.Email = request.Email?.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Address = request.Address?.Trim();
        customer.Notes = request.Notes?.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone,
            customer.Address, customer.Notes,
            customer.Vehicles.Count(v => !v.IsDeleted), customer.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var customer = await _db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
        if (customer is null) return false;

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

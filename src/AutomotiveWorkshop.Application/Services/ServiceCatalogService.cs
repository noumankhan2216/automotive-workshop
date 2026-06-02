using AutomotiveWorkshop.Application.DTOs.ServiceCatalog;
using AutomotiveWorkshop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceCatalogItemDto>> GetAllAsync(bool includeInactive, CancellationToken ct);
    Task<ServiceCatalogItemDto> CreateAsync(CreateServiceCatalogItemRequest request, CancellationToken ct);
    Task<ServiceCatalogItemDto?> UpdateAsync(Guid id, UpdateServiceCatalogItemRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly DbContext _db;

    public ServiceCatalogService(DbContext db) => _db = db;

    public async Task<IReadOnlyList<ServiceCatalogItemDto>> GetAllAsync(bool includeInactive, CancellationToken ct)
    {
        var query = _db.Set<ServiceCatalogItem>().AsNoTracking();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new ServiceCatalogItemDto(s.Id, s.Name, s.Description, s.DefaultPrice, s.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ServiceCatalogItemDto> CreateAsync(CreateServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = new ServiceCatalogItem
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DefaultPrice = request.DefaultPrice,
            IsActive = true
        };

        _db.Set<ServiceCatalogItem>().Add(item);
        await _db.SaveChangesAsync(ct);

        return new ServiceCatalogItemDto(item.Id, item.Name, item.Description, item.DefaultPrice, item.IsActive);
    }

    public async Task<ServiceCatalogItemDto?> UpdateAsync(Guid id, UpdateServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = await _db.Set<ServiceCatalogItem>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (item is null) return null;

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();
        item.DefaultPrice = request.DefaultPrice;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new ServiceCatalogItemDto(item.Id, item.Name, item.Description, item.DefaultPrice, item.IsActive);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await _db.Set<ServiceCatalogItem>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (item is null) return false;

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.Parts;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IPartService
{
    Task<PagedResult<PartDto>> GetAllAsync(string? search, bool? lowStockOnly, int page, int pageSize, CancellationToken ct);
    Task<PartDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PartDetailDto> CreateAsync(CreatePartRequest request, CancellationToken ct);
    Task<PartDetailDto?> UpdateAsync(Guid id, UpdatePartRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<PartDetailDto?> AdjustStockAsync(Guid id, AdjustPartStockRequest request, CancellationToken ct);
    Task<IReadOnlyList<PartStockTransactionDto>> GetTransactionsAsync(Guid id, int limit, CancellationToken ct);
}

public class PartService : IPartService
{
    private readonly DbContext _db;
    private readonly IValidator<CreatePartRequest> _createValidator;
    private readonly IValidator<UpdatePartRequest> _updateValidator;

    public PartService(
        DbContext db,
        IValidator<CreatePartRequest> createValidator,
        IValidator<UpdatePartRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<PartDto>> GetAllAsync(string? search, bool? lowStockOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Part>().AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term) ||
                (p.Category != null && p.Category.ToLower().Contains(term)));
        }

        if (lowStockOnly == true)
            query = query.Where(p => p.QuantityOnHand <= p.ReorderLevel);

        var total = await query.CountAsync(ct);
        var parts = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<PartDto>
        {
            Items = parts.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PartDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var part = await _db.Set<Part>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        return part is null ? null : MapToDetailDto(part);
    }

    public async Task<PartDetailDto> CreateAsync(CreatePartRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var part = new Part
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category?.Trim(),
            UnitCost = request.UnitCost,
            UnitPrice = request.UnitPrice,
            QuantityOnHand = request.QuantityOnHand,
            ReorderLevel = request.ReorderLevel
        };

        _db.Set<Part>().Add(part);
        await _db.SaveChangesAsync(ct);

        if (request.QuantityOnHand > 0)
        {
            _db.Set<PartStockTransaction>().Add(new PartStockTransaction
            {
                PartId = part.Id,
                Type = PartStockTransactionType.Receive,
                QuantityChange = request.QuantityOnHand,
                QuantityAfter = request.QuantityOnHand,
                Notes = "Initial stock"
            });
            await _db.SaveChangesAsync(ct);
        }

        return MapToDetailDto(part);
    }

    public async Task<PartDetailDto?> UpdateAsync(Guid id, UpdatePartRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var part = await _db.Set<Part>().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (part is null) return null;

        part.Sku = request.Sku.Trim();
        part.Name = request.Name.Trim();
        part.Description = request.Description?.Trim();
        part.Category = request.Category?.Trim();
        part.UnitCost = request.UnitCost;
        part.UnitPrice = request.UnitPrice;
        part.ReorderLevel = request.ReorderLevel;
        part.IsActive = request.IsActive;
        part.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDetailDto(part);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var part = await _db.Set<Part>().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (part is null) return false;

        part.IsDeleted = true;
        part.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PartDetailDto?> AdjustStockAsync(Guid id, AdjustPartStockRequest request, CancellationToken ct)
    {
        var part = await _db.Set<Part>().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (part is null) return null;

        var newQty = part.QuantityOnHand + request.QuantityChange;
        if (newQty < 0)
            throw new InvalidOperationException("Insufficient stock for this adjustment.");

        part.QuantityOnHand = newQty;
        part.UpdatedAt = DateTime.UtcNow;

        _db.Set<PartStockTransaction>().Add(new PartStockTransaction
        {
            PartId = part.Id,
            Type = request.Type,
            QuantityChange = request.QuantityChange,
            QuantityAfter = newQty,
            WorkOrderId = request.WorkOrderId,
            Notes = request.Notes?.Trim()
        });

        await _db.SaveChangesAsync(ct);
        return MapToDetailDto(part);
    }

    public async Task<IReadOnlyList<PartStockTransactionDto>> GetTransactionsAsync(Guid id, int limit, CancellationToken ct)
    {
        return await _db.Set<PartStockTransaction>().AsNoTracking()
            .Where(t => t.PartId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new PartStockTransactionDto(
                t.Id, t.Type, t.QuantityChange, t.QuantityAfter, t.Notes, t.CreatedAt))
            .ToListAsync(ct);
    }

    private static PartDto MapToDto(Part p) => new(
        p.Id, p.Sku, p.Name, p.Category, p.UnitCost, p.UnitPrice,
        p.QuantityOnHand, p.ReorderLevel, p.IsActive, p.QuantityOnHand <= p.ReorderLevel);

    private static PartDetailDto MapToDetailDto(Part p) => new(
        p.Id, p.Sku, p.Name, p.Description, p.Category, p.UnitCost, p.UnitPrice,
        p.QuantityOnHand, p.ReorderLevel, p.IsActive, p.QuantityOnHand <= p.ReorderLevel);
}

using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.Estimates;
using AutomotiveWorkshop.Application.DTOs.WorkOrders;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IEstimateService
{
    Task<PagedResult<EstimateDto>> GetAllAsync(EstimateStatus? status, int page, int pageSize, CancellationToken ct);
    Task<EstimateDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<EstimateDetailDto> CreateAsync(CreateEstimateRequest request, CancellationToken ct);
    Task<EstimateDetailDto?> UpdateAsync(Guid id, UpdateEstimateRequest request, CancellationToken ct);
    Task<EstimateDetailDto?> UpdateStatusAsync(Guid id, UpdateEstimateStatusRequest request, CancellationToken ct);
    Task<WorkOrderDetailDto?> ConvertToWorkOrderAsync(Guid id, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public class EstimateService : IEstimateService
{
    private readonly DbContext _db;
    private readonly IValidator<CreateEstimateRequest> _createValidator;
    private readonly IValidator<UpdateEstimateRequest> _updateValidator;
    private readonly INotificationService _notificationService;

    public EstimateService(
        DbContext db,
        IValidator<CreateEstimateRequest> createValidator,
        IValidator<UpdateEstimateRequest> updateValidator,
        INotificationService notificationService)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<EstimateDto>> GetAllAsync(EstimateStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Estimate>().AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Vehicle)
            .Include(e => e.Items)
            .Where(e => !e.IsDeleted);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var total = await query.CountAsync(ct);
        var estimates = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = estimates.Select(MapToDto).ToList();
        return new PagedResult<EstimateDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<EstimateDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var estimate = await _db.Set<Estimate>()
            .AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Vehicle)
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        return estimate is null ? null : MapToDetailDto(estimate);
    }

    public async Task<EstimateDetailDto> CreateAsync(CreateEstimateRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var settings = await _db.Set<WorkshopSettings>().FirstAsync(ct);
        var count = await _db.Set<Estimate>().CountAsync(ct);

        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            Status = EstimateStatus.Draft,
            TaxRate = settings.DefaultTaxRate,
            CustomerNotes = request.CustomerNotes?.Trim(),
            InternalNotes = request.InternalNotes?.Trim(),
            ValidUntil = request.ValidUntil ?? DateTime.UtcNow.AddDays(30),
            Items = request.Items.Select(i => new EstimateItem
            {
                ServiceCatalogItemId = i.ServiceCatalogItemId,
                Description = i.Description.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _db.Set<Estimate>().Add(estimate);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(estimate.Id, ct))!;
    }

    public async Task<EstimateDetailDto?> UpdateAsync(Guid id, UpdateEstimateRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var estimate = await _db.Set<Estimate>()
            .Include(e => e.Customer)
            .Include(e => e.Vehicle)
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        if (estimate is null) return null;

        if (estimate.Status is EstimateStatus.Converted)
            throw new InvalidOperationException("A converted estimate can no longer be edited.");

        estimate.CustomerNotes = request.CustomerNotes?.Trim();
        estimate.InternalNotes = request.InternalNotes?.Trim();
        estimate.ValidUntil = request.ValidUntil;
        estimate.UpdatedAt = DateTime.UtcNow;

        _db.Set<EstimateItem>().RemoveRange(estimate.Items);
        estimate.Items = request.Items.Select(i => new EstimateItem
        {
            EstimateId = estimate.Id,
            ServiceCatalogItemId = i.ServiceCatalogItemId,
            Description = i.Description.Trim(),
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(estimate.Id, ct))!;
    }

    public async Task<EstimateDetailDto?> UpdateStatusAsync(Guid id, UpdateEstimateStatusRequest request, CancellationToken ct)
    {
        var estimate = await _db.Set<Estimate>()
            .Include(e => e.Customer)
            .Include(e => e.Vehicle)
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        if (estimate is null) return null;

        if (estimate.Status == EstimateStatus.Converted)
            throw new InvalidOperationException("A converted estimate can no longer change status.");

        estimate.Status = request.Status;
        estimate.UpdatedAt = DateTime.UtcNow;
        estimate.ApprovedAt = request.Status == EstimateStatus.Approved ? DateTime.UtcNow : estimate.ApprovedAt;

        await _db.SaveChangesAsync(ct);

        if (request.Status == EstimateStatus.Sent && !string.IsNullOrWhiteSpace(estimate.Customer.Email))
        {
            var total = estimate.Items.Sum(i => i.LineTotal) * (1 + estimate.TaxRate);
            await _notificationService.SendEmailAsync(
                estimate.Customer.Email,
                $"Estimate {estimate.EstimateNumber}",
                $"Please review your estimate totalling {total:C}. It is valid until {estimate.ValidUntil:d}.",
                ct);
        }

        return MapToDetailDto(estimate);
    }

    public async Task<WorkOrderDetailDto?> ConvertToWorkOrderAsync(Guid id, CancellationToken ct)
    {
        var estimate = await _db.Set<Estimate>()
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        if (estimate is null) return null;

        if (estimate.Status == EstimateStatus.Converted && estimate.ConvertedWorkOrderId.HasValue)
            return await LoadWorkOrderDetail(estimate.ConvertedWorkOrderId.Value, ct);

        if (estimate.Status != EstimateStatus.Approved)
            throw new InvalidOperationException("Only an approved estimate can be converted to a work order.");

        var count = await _db.Set<WorkOrder>().CountAsync(ct);
        var workOrder = new WorkOrder
        {
            WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            CustomerId = estimate.CustomerId,
            VehicleId = estimate.VehicleId,
            EstimateId = estimate.Id,
            Status = WorkOrderStatus.Draft,
            CustomerNotes = estimate.CustomerNotes,
            InternalNotes = estimate.InternalNotes,
            Items = estimate.Items.Select(i => new WorkOrderItem
            {
                ServiceCatalogItemId = i.ServiceCatalogItemId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        estimate.Status = EstimateStatus.Converted;
        estimate.ConvertedWorkOrderId = workOrder.Id;
        estimate.UpdatedAt = DateTime.UtcNow;

        _db.Set<WorkOrder>().Add(workOrder);
        await _db.SaveChangesAsync(ct);

        return await LoadWorkOrderDetail(workOrder.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var estimate = await _db.Set<Estimate>().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);
        if (estimate is null) return false;

        if (estimate.Status == EstimateStatus.Converted)
            throw new InvalidOperationException("A converted estimate cannot be deleted.");

        estimate.IsDeleted = true;
        estimate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<WorkOrderDetailDto?> LoadWorkOrderDetail(Guid workOrderId, CancellationToken ct)
    {
        var w = await _db.Set<WorkOrder>()
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == workOrderId, ct);

        if (w is null) return null;

        return new WorkOrderDetailDto(
            w.Id, w.WorkOrderNumber, w.CustomerId, w.Customer.Name, w.VehicleId,
            $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
            w.EstimateId,
            w.Status, w.AssignedToUserId, w.CustomerNotes, w.InternalNotes,
            w.OpenedAt, w.CompletedAt,
            w.Items.Select(i => new WorkOrderItemDto(
                i.Id, i.ServiceCatalogItemId, i.Description, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
            w.Items.Sum(i => i.LineTotal));
    }

    private static EstimateDto MapToDto(Estimate e) => new(
        e.Id, e.EstimateNumber, e.CustomerId, e.Customer.Name, e.VehicleId,
        $"{e.Vehicle.Year} {e.Vehicle.Make} {e.Vehicle.Model}",
        e.Status, e.CreatedAt, e.ValidUntil, e.ConvertedWorkOrderId,
        e.Items.Sum(i => i.LineTotal));

    private static EstimateDetailDto MapToDetailDto(Estimate e)
    {
        var subTotal = e.Items.Sum(i => i.LineTotal);
        var taxAmount = Math.Round(subTotal * e.TaxRate, 2);
        return new EstimateDetailDto(
            e.Id, e.EstimateNumber, e.CustomerId, e.Customer.Name,
            e.Customer.Email, e.Customer.Phone, e.Customer.Address,
            e.VehicleId, $"{e.Vehicle.Year} {e.Vehicle.Make} {e.Vehicle.Model}",
            e.Vehicle.Vin, e.Vehicle.LicensePlate,
            e.Status, e.CustomerNotes, e.InternalNotes,
            e.CreatedAt, e.ValidUntil, e.ApprovedAt, e.ConvertedWorkOrderId,
            e.Items.Select(i => new EstimateItemDto(
                i.Id, i.ServiceCatalogItemId, i.Description, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
            subTotal, e.TaxRate, taxAmount, subTotal + taxAmount);
    }
}

using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.WorkOrders;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IWorkOrderService
{
    Task<PagedResult<WorkOrderDto>> GetAllAsync(WorkOrderStatus? status, int page, int pageSize, CancellationToken ct);
    Task<WorkOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<WorkOrderDetailDto> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct);
    Task<WorkOrderDetailDto?> UpdateStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken ct);
}

public class WorkOrderService : IWorkOrderService
{
    private readonly DbContext _db;
    private readonly IValidator<CreateWorkOrderRequest> _createValidator;

    public WorkOrderService(DbContext db, IValidator<CreateWorkOrderRequest> createValidator)
    {
        _db = db;
        _createValidator = createValidator;
    }

    public async Task<PagedResult<WorkOrderDto>> GetAllAsync(WorkOrderStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<WorkOrder>().AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items)
            .Where(w => !w.IsDeleted);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        var total = await query.CountAsync(ct);
        var workOrders = await query
            .OrderByDescending(w => w.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = workOrders.Select(MapToDto).ToList();

        return new PagedResult<WorkOrderDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<WorkOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var workOrder = await _db.Set<WorkOrder>()
            .AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

        return workOrder is null ? null : MapToDetailDto(workOrder);
    }

    public async Task<WorkOrderDetailDto> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var count = await _db.Set<WorkOrder>().CountAsync(ct);
        var workOrder = new WorkOrder
        {
            WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}",
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            CustomerNotes = request.CustomerNotes?.Trim(),
            InternalNotes = request.InternalNotes?.Trim(),
            Status = WorkOrderStatus.Draft,
            Items = request.Items.Select(i => new WorkOrderItem
            {
                ServiceCatalogItemId = i.ServiceCatalogItemId,
                Description = i.Description.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _db.Set<WorkOrder>().Add(workOrder);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(workOrder.Id, ct))!;
    }

    public async Task<WorkOrderDetailDto?> UpdateStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken ct)
    {
        var workOrder = await _db.Set<WorkOrder>()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

        if (workOrder is null) return null;

        workOrder.Status = request.Status;
        workOrder.UpdatedAt = DateTime.UtcNow;

        if (request.Status == WorkOrderStatus.Completed)
            workOrder.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDetailDto(workOrder);
    }

    private static WorkOrderDto MapToDto(WorkOrder w) => new(
        w.Id, w.WorkOrderNumber, w.CustomerId, w.Customer.Name, w.VehicleId,
        $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
        w.Status, w.AssignedToUserId, w.OpenedAt, w.CompletedAt,
        w.Items.Sum(i => i.LineTotal));

    private static WorkOrderDetailDto MapToDetailDto(WorkOrder w) => new(
        w.Id, w.WorkOrderNumber, w.CustomerId, w.Customer.Name, w.VehicleId,
        $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
        w.EstimateId,
        w.Status, w.AssignedToUserId, w.CustomerNotes, w.InternalNotes,
        w.OpenedAt, w.CompletedAt,
        w.Items.Select(i => new WorkOrderItemDto(
            i.Id, i.ServiceCatalogItemId, i.Description, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
        w.Items.Sum(i => i.LineTotal));
}

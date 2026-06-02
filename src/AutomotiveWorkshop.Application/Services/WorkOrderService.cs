using AutomotiveWorkshop.Application.Common;
using AutomotiveWorkshop.Application.DTOs.TimeTracking;
using AutomotiveWorkshop.Application.DTOs.WorkOrders;
using AutomotiveWorkshop.Application.Interfaces;
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
    Task<IssuePartsResultDto?> IssuePartsAsync(Guid id, CancellationToken ct);
}

public class WorkOrderService : IWorkOrderService
{
    private readonly DbContext _db;
    private readonly IUserDirectoryService _users;
    private readonly IValidator<CreateWorkOrderRequest> _createValidator;

    public WorkOrderService(
        DbContext db,
        IUserDirectoryService users,
        IValidator<CreateWorkOrderRequest> createValidator)
    {
        _db = db;
        _users = users;
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

        var items = new List<WorkOrderDto>();
        foreach (var w in workOrders)
        {
            var name = await _users.GetDisplayNameAsync(w.AssignedToUserId, ct);
            items.Add(MapToDto(w, name));
        }

        return new PagedResult<WorkOrderDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<WorkOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var workOrder = await _db.Set<WorkOrder>()
            .AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items).ThenInclude(i => i.Part)
            .Include(w => w.TimeEntries)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

        return workOrder is null ? null : await MapToDetailDtoAsync(workOrder, ct);
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
                PartId = i.PartId,
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
            .Include(w => w.Items).ThenInclude(i => i.Part)
            .Include(w => w.TimeEntries)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

        if (workOrder is null) return null;

        workOrder.Status = request.Status;
        workOrder.UpdatedAt = DateTime.UtcNow;

        if (request.Status == WorkOrderStatus.Completed)
            workOrder.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await MapToDetailDtoAsync(workOrder, ct);
    }

    public async Task<IssuePartsResultDto?> IssuePartsAsync(Guid id, CancellationToken ct)
    {
        var workOrder = await _db.Set<WorkOrder>()
            .Include(w => w.Items).ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

        if (workOrder is null) return null;

        var messages = new List<string>();
        var issued = 0;

        foreach (var line in workOrder.Items.Where(i => i.PartId.HasValue && !i.PartsIssued))
        {
            var part = line.Part ?? await _db.Set<Part>().FirstOrDefaultAsync(p => p.Id == line.PartId && !p.IsDeleted, ct);
            if (part is null)
            {
                messages.Add($"Part not found for line: {line.Description}");
                continue;
            }

            var qty = -line.Quantity;
            if (part.QuantityOnHand + qty < 0)
            {
                messages.Add($"Insufficient stock for {part.Sku} (need {line.Quantity}, have {part.QuantityOnHand})");
                continue;
            }

            part.QuantityOnHand += qty;
            part.UpdatedAt = DateTime.UtcNow;
            line.PartsIssued = true;

            _db.Set<PartStockTransaction>().Add(new PartStockTransaction
            {
                PartId = part.Id,
                Type = PartStockTransactionType.Issue,
                QuantityChange = qty,
                QuantityAfter = part.QuantityOnHand,
                WorkOrderId = workOrder.Id,
                Notes = $"Issued to {workOrder.WorkOrderNumber}: {line.Description}"
            });

            issued++;
            messages.Add($"Issued {line.Quantity}× {part.Sku}");
        }

        if (issued == 0 && messages.Count == 0)
            messages.Add("No part lines to issue (link parts on line items first).");

        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new IssuePartsResultDto(issued, messages);
    }

    private static WorkOrderDto MapToDto(WorkOrder w, string? assignedName) => new(
        w.Id, w.WorkOrderNumber, w.CustomerId, w.Customer.Name, w.VehicleId,
        $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
        w.Status, w.AssignedToUserId, assignedName,
        w.ScheduledStartAt, w.ScheduledEndAt, w.BayLabel,
        w.OpenedAt, w.CompletedAt,
        w.Items.Sum(i => i.LineTotal));

    private async Task<WorkOrderDetailDto> MapToDetailDtoAsync(WorkOrder w, CancellationToken ct)
    {
        var assignedName = await _users.GetDisplayNameAsync(w.AssignedToUserId, ct);
        var timeDtos = new List<TimeEntryDto>();
        foreach (var e in w.TimeEntries.OrderByDescending(t => t.StartedAt))
        {
            var userName = await _users.GetDisplayNameAsync(e.UserId, ct) ?? "Unknown";
            timeDtos.Add(new TimeEntryDto(
                e.Id, e.WorkOrderId, e.UserId, userName, e.StartedAt, e.EndedAt,
                e.Hours is null ? null : Math.Round(e.Hours.Value, 2),
                e.Notes, !e.EndedAt.HasValue));
        }

        var totalHours = timeDtos.Where(t => t.Hours.HasValue).Sum(t => t.Hours!.Value);

        return new WorkOrderDetailDto(
            w.Id, w.WorkOrderNumber, w.CustomerId, w.Customer.Name, w.VehicleId,
            $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
            w.EstimateId,
            w.Status, w.AssignedToUserId, assignedName,
            w.ScheduledStartAt, w.ScheduledEndAt, w.BayLabel,
            w.CustomerNotes, w.InternalNotes,
            w.OpenedAt, w.CompletedAt,
            w.Items.Select(MapItemDto).ToList(),
            timeDtos,
            Math.Round(totalHours, 2),
            w.Items.Sum(i => i.LineTotal));
    }

    private static WorkOrderItemDto MapItemDto(WorkOrderItem i) => new(
        i.Id, i.ServiceCatalogItemId, i.PartId, i.Part?.Sku, i.Part?.Name, i.PartsIssued,
        i.Description, i.Quantity, i.UnitPrice, i.LineTotal);
}

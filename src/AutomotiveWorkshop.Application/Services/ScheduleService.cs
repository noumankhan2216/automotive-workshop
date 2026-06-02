using AutomotiveWorkshop.Application.DTOs.Schedule;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleEventDto>> GetEventsAsync(DateTime from, DateTime to, CancellationToken ct);
    Task<ScheduleEventDto?> UpdateScheduleAsync(Guid workOrderId, UpdateWorkOrderScheduleRequest request, CancellationToken ct);
    Task<ScheduleEventDto?> AssignTechnicianAsync(Guid workOrderId, AssignWorkOrderRequest request, CancellationToken ct);
}

public class ScheduleService : IScheduleService
{
    private readonly DbContext _db;
    private readonly IUserDirectoryService _users;

    public ScheduleService(DbContext db, IUserDirectoryService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<ScheduleEventDto>> GetEventsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var workOrders = await _db.Set<WorkOrder>().AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items)
            .Where(w => !w.IsDeleted &&
                        w.ScheduledStartAt != null &&
                        w.ScheduledEndAt != null &&
                        w.ScheduledStartAt < to &&
                        w.ScheduledEndAt > from &&
                        w.Status != WorkOrderStatus.Cancelled)
            .OrderBy(w => w.ScheduledStartAt)
            .ToListAsync(ct);

        var events = new List<ScheduleEventDto>();
        foreach (var w in workOrders)
        {
            var name = await _users.GetDisplayNameAsync(w.AssignedToUserId, ct);
            events.Add(MapEvent(w, name));
        }

        return events;
    }

    public async Task<ScheduleEventDto?> UpdateScheduleAsync(Guid workOrderId, UpdateWorkOrderScheduleRequest request, CancellationToken ct)
    {
        if (request.ScheduledEndAt <= request.ScheduledStartAt)
            throw new InvalidOperationException("Scheduled end must be after start.");

        var workOrder = await LoadForUpdate(workOrderId, ct);
        if (workOrder is null) return null;

        workOrder.ScheduledStartAt = request.ScheduledStartAt;
        workOrder.ScheduledEndAt = request.ScheduledEndAt;
        workOrder.BayLabel = request.BayLabel?.Trim();
        if (request.AssignedToUserId is not null)
            workOrder.AssignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId) ? null : request.AssignedToUserId;

        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var name = await _users.GetDisplayNameAsync(workOrder.AssignedToUserId, ct);
        return MapEvent(workOrder, name);
    }

    public async Task<ScheduleEventDto?> AssignTechnicianAsync(Guid workOrderId, AssignWorkOrderRequest request, CancellationToken ct)
    {
        var workOrder = await LoadForUpdate(workOrderId, ct);
        if (workOrder is null) return null;

        workOrder.AssignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId) ? null : request.AssignedToUserId;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (workOrder.ScheduledStartAt is null || workOrder.ScheduledEndAt is null)
            return null;

        var name = await _users.GetDisplayNameAsync(workOrder.AssignedToUserId, ct);
        return MapEvent(workOrder, name);
    }

    private async Task<WorkOrder?> LoadForUpdate(Guid id, CancellationToken ct) =>
        await _db.Set<WorkOrder>()
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

    private static ScheduleEventDto MapEvent(WorkOrder w, string? assignedName) => new(
        w.Id,
        w.WorkOrderNumber,
        w.Customer.Name,
        $"{w.Vehicle.Year} {w.Vehicle.Make} {w.Vehicle.Model}",
        w.Status,
        w.AssignedToUserId,
        assignedName,
        w.BayLabel,
        w.ScheduledStartAt!.Value,
        w.ScheduledEndAt!.Value,
        w.Items.Sum(i => i.LineTotal));
}

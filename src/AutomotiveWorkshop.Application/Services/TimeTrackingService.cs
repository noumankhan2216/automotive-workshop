using AutomotiveWorkshop.Application.DTOs.TimeTracking;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Application.Services;

public interface ITimeTrackingService
{
    Task<IReadOnlyList<TimeEntryDto>> GetForWorkOrderAsync(Guid workOrderId, CancellationToken ct);
    Task<TimeEntryDto?> ClockInAsync(Guid workOrderId, ClockInRequest request, string currentUserId, CancellationToken ct);
    Task<TimeEntryDto?> ClockOutAsync(Guid timeEntryId, ClockOutRequest request, CancellationToken ct);
}

public class TimeTrackingService : ITimeTrackingService
{
    private readonly DbContext _db;
    private readonly IUserDirectoryService _users;

    public TimeTrackingService(DbContext db, IUserDirectoryService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<TimeEntryDto>> GetForWorkOrderAsync(Guid workOrderId, CancellationToken ct)
    {
        var entries = await _db.Set<TimeEntry>().AsNoTracking()
            .Where(t => t.WorkOrderId == workOrderId)
            .OrderByDescending(t => t.StartedAt)
            .ToListAsync(ct);

        var result = new List<TimeEntryDto>();
        foreach (var e in entries)
        {
            var name = await _users.GetDisplayNameAsync(e.UserId, ct) ?? "Unknown";
            result.Add(Map(e, name));
        }

        return result;
    }

    public async Task<TimeEntryDto?> ClockInAsync(Guid workOrderId, ClockInRequest request, string currentUserId, CancellationToken ct)
    {
        var workOrderExists = await _db.Set<WorkOrder>().AnyAsync(w => w.Id == workOrderId && !w.IsDeleted, ct);
        if (!workOrderExists) return null;

        var userId = string.IsNullOrWhiteSpace(request.UserId) ? currentUserId : request.UserId;

        var hasOpen = await _db.Set<TimeEntry>().AnyAsync(
            t => t.WorkOrderId == workOrderId && t.UserId == userId && t.EndedAt == null, ct);
        if (hasOpen)
            throw new InvalidOperationException("This technician already has an open time entry on this work order.");

        var entry = new TimeEntry
        {
            WorkOrderId = workOrderId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim()
        };

        _db.Set<TimeEntry>().Add(entry);
        await _db.SaveChangesAsync(ct);

        var name = await _users.GetDisplayNameAsync(userId, ct) ?? "Unknown";
        return Map(entry, name);
    }

    public async Task<TimeEntryDto?> ClockOutAsync(Guid timeEntryId, ClockOutRequest request, CancellationToken ct)
    {
        var entry = await _db.Set<TimeEntry>().FirstOrDefaultAsync(t => t.Id == timeEntryId, ct);
        if (entry is null) return null;

        if (entry.EndedAt.HasValue)
            throw new InvalidOperationException("This time entry is already closed.");

        entry.EndedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            entry.Notes = request.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        var name = await _users.GetDisplayNameAsync(entry.UserId, ct) ?? "Unknown";
        return Map(entry, name);
    }

    private static TimeEntryDto Map(TimeEntry e, string userName) => new(
        e.Id,
        e.WorkOrderId,
        e.UserId,
        userName,
        e.StartedAt,
        e.EndedAt,
        e.Hours is null ? null : Math.Round(e.Hours.Value, 2),
        e.Notes,
        !e.EndedAt.HasValue);
}

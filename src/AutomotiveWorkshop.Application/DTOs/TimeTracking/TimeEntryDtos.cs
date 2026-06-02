namespace AutomotiveWorkshop.Application.DTOs.TimeTracking;

public record TimeEntryDto(
    Guid Id,
    Guid WorkOrderId,
    string UserId,
    string UserName,
    DateTime StartedAt,
    DateTime? EndedAt,
    decimal? Hours,
    string? Notes,
    bool IsActive);

public record ClockInRequest(string? UserId, string? Notes);

public record ClockOutRequest(string? Notes);

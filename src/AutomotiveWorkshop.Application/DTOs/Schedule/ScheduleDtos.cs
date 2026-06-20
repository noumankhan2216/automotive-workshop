using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Application.DTOs.Schedule;

public record ScheduleEventDto(
    Guid WorkOrderId,
    string WorkOrderNumber,
    string CustomerName,
    string VehicleDescription,
    WorkOrderStatus Status,
    string? AssignedToUserId,
    string? AssignedToUserName,
    string? BayLabel,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    decimal TotalAmount);

public record UpdateWorkOrderScheduleRequest(
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    string? BayLabel,
    string? AssignedToUserId);

public record AssignWorkOrderRequest(string? AssignedToUserId);

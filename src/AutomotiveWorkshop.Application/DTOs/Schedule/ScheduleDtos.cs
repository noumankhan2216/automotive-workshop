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
    DateTime ScheduledStartAt,
    DateTime ScheduledEndAt,
    decimal TotalAmount);

public record UpdateWorkOrderScheduleRequest(
    DateTime ScheduledStartAt,
    DateTime ScheduledEndAt,
    string? BayLabel,
    string? AssignedToUserId);

public record AssignWorkOrderRequest(string? AssignedToUserId);

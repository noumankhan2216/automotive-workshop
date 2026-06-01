using AutomotiveWorkshop.Domain.Common;
using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Domain.Entities;

public class WorkOrder : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkOrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public string? AssignedToUserId { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<WorkOrderItem> Items { get; set; } = [];
    public Invoice? Invoice { get; set; }
}

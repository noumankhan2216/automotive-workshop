namespace AutomotiveWorkshop.Domain.Entities;

public class TimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public decimal? Hours =>
        EndedAt.HasValue ? (decimal)(EndedAt.Value - StartedAt).TotalHours : null;
}

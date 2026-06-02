using AutomotiveWorkshop.Domain.Common;
using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Domain.Entities;

public class Estimate : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EstimateNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;
    public decimal TaxRate { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ConvertedWorkOrderId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<EstimateItem> Items { get; set; } = [];
}

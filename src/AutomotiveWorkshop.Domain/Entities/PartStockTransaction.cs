using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Domain.Entities;

public class PartStockTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartId { get; set; }
    public PartStockTransactionType Type { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal QuantityAfter { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public Part Part { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}

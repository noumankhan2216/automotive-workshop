using AutomotiveWorkshop.Domain.Common;

namespace AutomotiveWorkshop.Domain.Entities;

public class Part : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PartStockTransaction> StockTransactions { get; set; } = [];
}

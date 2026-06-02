namespace AutomotiveWorkshop.Domain.Entities;

public class WorkOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public Guid? PartId { get; set; }
    public bool PartsIssued { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public WorkOrder WorkOrder { get; set; } = null!;
    public ServiceCatalogItem? ServiceCatalogItem { get; set; }
    public Part? Part { get; set; }
}

using AutomotiveWorkshop.Domain.Common;

namespace AutomotiveWorkshop.Domain.Entities;

public class ServiceCatalogItem : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WorkOrderItem> WorkOrderItems { get; set; } = [];
}

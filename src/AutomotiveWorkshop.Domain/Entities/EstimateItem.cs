namespace AutomotiveWorkshop.Domain.Entities;

public class EstimateItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EstimateId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public Estimate Estimate { get; set; } = null!;
    public ServiceCatalogItem? ServiceCatalogItem { get; set; }
}

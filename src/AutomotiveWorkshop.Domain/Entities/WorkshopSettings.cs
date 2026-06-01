namespace AutomotiveWorkshop.Domain.Entities;

public class WorkshopSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkshopName { get; set; } = "Automotive Workshop";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal DefaultTaxRate { get; set; } = 0.10m;
    public string CurrencyCode { get; set; } = "USD";
}

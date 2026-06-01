using AutomotiveWorkshop.Domain.Common;

namespace AutomotiveWorkshop.Domain.Entities;

public class Vehicle : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Vin { get; set; }
    public string? LicensePlate { get; set; }
    public int? Mileage { get; set; }
    public string? Color { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<WorkOrder> WorkOrders { get; set; } = [];
}

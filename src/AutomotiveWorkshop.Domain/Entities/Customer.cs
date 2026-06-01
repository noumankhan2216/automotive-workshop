using AutomotiveWorkshop.Domain.Common;

namespace AutomotiveWorkshop.Domain.Entities;

public class Customer : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<WorkOrder> WorkOrders { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}

using AutomotiveWorkshop.Domain.Common;
using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Domain.Entities;

public class Invoice : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = [];
}

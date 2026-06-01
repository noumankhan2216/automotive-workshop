using AutomotiveWorkshop.Domain.Enums;

namespace AutomotiveWorkshop.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}

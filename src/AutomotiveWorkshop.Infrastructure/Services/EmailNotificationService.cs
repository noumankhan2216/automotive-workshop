using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using AutomotiveWorkshop.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AutomotiveWorkshop.Infrastructure.Services;

public class EmailNotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ApplicationDbContext db, ILogger<EmailNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var log = new NotificationLog
        {
            Channel = NotificationChannel.Email,
            Recipient = to,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow
        };

        // MVP: log only. Wire SendGrid/SMTP in production.
        _logger.LogInformation("Email queued to {Recipient}: {Subject}", to, subject);

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

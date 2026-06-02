using System.Net;
using System.Net.Mail;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using AutomotiveWorkshop.Infrastructure.Configuration;
using AutomotiveWorkshop.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutomotiveWorkshop.Infrastructure.Services;

public class EmailNotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ApplicationDbContext db,
        IOptions<EmailSettings> settings,
        ILogger<EmailNotificationService> logger)
    {
        _db = db;
        _settings = settings.Value;
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
            Status = NotificationStatus.Pending
        };

        try
        {
            if (_settings.IsConfigured)
            {
                await SendViaSmtpAsync(to, subject, body, cancellationToken);
                _logger.LogInformation("Email sent to {Recipient}: {Subject}", to, subject);
            }
            else
            {
                _logger.LogInformation("Email (SMTP not configured, logged only) to {Recipient}: {Subject}", to, subject);
            }

            log.Status = NotificationStatus.Sent;
            log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
        }

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SendViaSmtpAsync(string to, string subject, string body, CancellationToken ct)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.Username, _settings.Password)
        };

        await client.SendMailAsync(message, ct);
    }
}

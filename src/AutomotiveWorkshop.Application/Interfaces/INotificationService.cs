namespace AutomotiveWorkshop.Application.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

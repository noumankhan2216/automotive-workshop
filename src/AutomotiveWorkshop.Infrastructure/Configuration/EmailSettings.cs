namespace AutomotiveWorkshop.Infrastructure.Configuration;

public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>SMTP host. When empty, emails are logged only (no network send).</summary>
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@workshop.local";
    public string FromName { get; set; } = "Automotive Workshop";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}

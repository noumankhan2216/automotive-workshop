namespace AutomotiveWorkshop.Infrastructure.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AutomotiveWorkshop";
    public string Audience { get; set; } = "AutomotiveWorkshop";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
}

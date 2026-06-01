namespace AutomotiveWorkshop.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(string userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
}

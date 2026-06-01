namespace AutomotiveWorkshop.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles);

public record RefreshTokenRequest(string RefreshToken);

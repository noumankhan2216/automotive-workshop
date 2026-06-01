using AutomotiveWorkshop.Application.DTOs.Auth;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Infrastructure.Configuration;
using AutomotiveWorkshop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return Ok(new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            new UserDto(user.Id, user.Email!, user.FullName, roles.ToList())));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto(user.Id, user.Email!, user.FullName, roles.ToList()));
    }
}

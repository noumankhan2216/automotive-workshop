using AutomotiveWorkshop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserDirectoryService _users;

    public UsersController(IUserDirectoryService users) => _users = users;

    [HttpGet("technicians")]
    public async Task<IActionResult> GetTechnicians(CancellationToken ct)
        => Ok(await _users.GetTechniciansAsync(ct));
}

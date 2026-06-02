using AutomotiveWorkshop.Application.DTOs.Reports;
using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Infrastructure.Services;

public class UserDirectoryService : IUserDirectoryService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserDirectoryService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IReadOnlyList<TechnicianUserDto>> GetTechniciansAsync(CancellationToken ct)
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        var technicians = new List<TechnicianUserDto>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "Technician") ||
                await _userManager.IsInRoleAsync(user, "Manager") ||
                await _userManager.IsInRoleAsync(user, "Admin"))
            {
                technicians.Add(new TechnicianUserDto(user.Id, user.FullName, user.Email ?? user.UserName ?? ""));
            }
        }

        return technicians;
    }

    public async Task<string?> GetDisplayNameAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName;
    }
}

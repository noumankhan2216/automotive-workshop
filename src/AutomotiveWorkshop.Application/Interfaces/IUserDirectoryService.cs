using AutomotiveWorkshop.Application.DTOs.Reports;

namespace AutomotiveWorkshop.Application.Interfaces;

public interface IUserDirectoryService
{
    Task<IReadOnlyList<TechnicianUserDto>> GetTechniciansAsync(CancellationToken ct);
    Task<string?> GetDisplayNameAsync(string? userId, CancellationToken ct);
}

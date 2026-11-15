using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

public interface IStaffService
{
    /// <summary>The staff of the admin's agency, by name (FR-13).</summary>
    Task<IReadOnlyList<StaffViewModel>> ListAsync(Guid adminId, CancellationToken cancellationToken);

    Task<StaffViewModel> CreateAsync(Guid adminId, CreateStaffInputModel input, CancellationToken cancellationToken);

    Task<StaffViewModel> ChangeRoleAsync(Guid adminId, Guid userId, ChangeRoleInputModel input, CancellationToken cancellationToken);
}

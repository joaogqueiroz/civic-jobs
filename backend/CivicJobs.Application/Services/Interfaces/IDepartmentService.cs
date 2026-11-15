using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

public interface IDepartmentService
{
    /// <summary>The departments of the admin's agency, by name (FR-12).</summary>
    Task<IReadOnlyList<DepartmentViewModel>> ListAsync(Guid adminId, CancellationToken cancellationToken);

    Task<DepartmentViewModel> CreateAsync(Guid adminId, CreateDepartmentInputModel input, CancellationToken cancellationToken);
}

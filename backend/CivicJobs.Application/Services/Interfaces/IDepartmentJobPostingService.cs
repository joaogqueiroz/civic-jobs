using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.Services.Interfaces;

/// <summary>The hiring manager's view of the postings of their department (FR-15 to FR-17, BR-16).</summary>
public interface IDepartmentJobPostingService
{
    Task<IReadOnlyList<JobPostingViewModel>> ListAsync(Guid managerId, JobPostingStatus? status, CancellationToken cancellationToken);

    Task<JobPostingViewModel> GetByIdAsync(Guid managerId, Guid id, CancellationToken cancellationToken);

    Task<JobPostingViewModel> ApproveAsync(Guid managerId, Guid id, CancellationToken cancellationToken);

    Task<JobPostingViewModel> RejectAsync(
        Guid managerId,
        Guid id,
        RejectJobPostingInputModel input,
        CancellationToken cancellationToken);
}

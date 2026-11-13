using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

/// <summary>The hiring manager's side of applications to their department's postings (FR-20, FR-21).</summary>
public interface IDepartmentApplicationService
{
    Task<IReadOnlyList<PostingApplicationViewModel>> ListByPostingAsync(
        Guid managerId,
        Guid jobPostingId,
        CancellationToken cancellationToken);

    Task<PostingApplicationViewModel> ChangeStatusAsync(
        Guid managerId,
        Guid applicationId,
        ChangeApplicationStatusInputModel input,
        CancellationToken cancellationToken);
}

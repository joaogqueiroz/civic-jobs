using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.Services;

public interface IJobPostingService
{
    Task<JobPostingViewModel> CreateAsync(Guid userId, CreateJobPostingInputModel input, CancellationToken cancellationToken);

    Task<JobPostingViewModel> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobPostingViewModel>> ListAsync(Guid userId, JobPostingStatus? status, CancellationToken cancellationToken);

    Task<JobPostingViewModel> UpdateAsync(Guid userId, Guid id, UpdateJobPostingInputModel input, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<JobPostingViewModel> PublishAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<JobPostingViewModel> CloseAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    /// <summary>Open postings from every recruiter that close today or later, for anonymous candidates (FR-11).</summary>
    Task<IReadOnlyList<PublicJobPostingViewModel>> ListPublicAsync(CancellationToken cancellationToken);
}

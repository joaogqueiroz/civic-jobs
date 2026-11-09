using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services;

public interface IJobPostingService
{
    Task<JobPostingViewModel> CreateAsync(Guid userId, CreateJobPostingInputModel input, CancellationToken cancellationToken);
}

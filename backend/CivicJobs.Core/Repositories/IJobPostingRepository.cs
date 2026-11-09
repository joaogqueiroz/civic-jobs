using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IJobPostingRepository
{
    Task AddAsync(JobPosting posting, CancellationToken cancellationToken);
}

using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when the candidate already applied to the posting (BR-18).</summary>
    Task<bool> ExistsAsync(Guid jobPostingId, Guid candidateId, CancellationToken cancellationToken);

    /// <summary>The candidate's applications, newest first (FR-19).</summary>
    Task<IReadOnlyList<JobApplication>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken);

    /// <summary>The applications to a posting, oldest first (FR-20).</summary>
    Task<IReadOnlyList<JobApplication>> ListByPostingAsync(Guid jobPostingId, CancellationToken cancellationToken);

    Task AddAsync(JobApplication application, CancellationToken cancellationToken);

    Task UpdateAsync(JobApplication application, CancellationToken cancellationToken);
}

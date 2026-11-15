using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Core.Repositories;

public interface IJobPostingRepository
{
    Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobPosting>> ListByOwnerAsync(
        Guid ownerId,
        JobPostingStatus? status,
        CancellationToken cancellationToken);

    /// <summary>Postings of a department except drafts, newest first, for its hiring manager (FR-15).</summary>
    Task<IReadOnlyList<JobPosting>> ListByDepartmentAsync(
        Guid departmentId,
        JobPostingStatus? status,
        CancellationToken cancellationToken);

    /// <summary>Open postings from every recruiter whose closing date is on or after the given date (FR-11).</summary>
    Task<IReadOnlyList<JobPosting>> ListOpenAsync(DateOnly closingOnOrAfter, CancellationToken cancellationToken);

    /// <summary>Open postings whose closing date is before the given date, tracked so they can be closed (BR-11).</summary>
    Task<IReadOnlyList<JobPosting>> ListExpiredOpenAsync(DateOnly today, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobPosting>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task AddAsync(JobPosting posting, CancellationToken cancellationToken);

    Task UpdateAsync(JobPosting posting, CancellationToken cancellationToken);

    Task DeleteAsync(JobPosting posting, CancellationToken cancellationToken);
}

using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence.Repositories;

public sealed class JobPostingRepository(CivicJobsDbContext context) : IJobPostingRepository
{
    public Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.JobPostings.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobPosting>> ListByOwnerAsync(
        Guid ownerId,
        JobPostingStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.JobPostings.AsNoTracking().Where(p => p.CreatedByUserId == ownerId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobPosting posting, CancellationToken cancellationToken)
    {
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JobPosting posting, CancellationToken cancellationToken)
    {
        context.JobPostings.Update(posting);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(JobPosting posting, CancellationToken cancellationToken)
    {
        context.JobPostings.Remove(posting);
        await context.SaveChangesAsync(cancellationToken);
    }
}

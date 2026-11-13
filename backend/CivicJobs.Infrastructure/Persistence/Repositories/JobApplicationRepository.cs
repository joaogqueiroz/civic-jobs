using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence.Repositories;

public sealed class JobApplicationRepository(CivicJobsDbContext context) : IJobApplicationRepository
{
    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteUniqueConstraintError = 2067;

    public Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.JobApplications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid jobPostingId, Guid candidateId, CancellationToken cancellationToken) =>
        context.JobApplications.AnyAsync(a => a.JobPostingId == jobPostingId && a.CandidateId == candidateId, cancellationToken);

    public async Task<IReadOnlyList<JobApplication>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken) =>
        await context.JobApplications
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JobApplication>> ListByPostingAsync(Guid jobPostingId, CancellationToken cancellationToken) =>
        await context.JobApplications
            .AsNoTracking()
            .Where(a => a.JobPostingId == jobPostingId)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobApplication application, CancellationToken cancellationToken)
    {
        context.JobApplications.Add(application);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintError })
        {
            // Two Apply clicks raced past the service check; the unique index caught the second one.
            context.Entry(application).State = EntityState.Detached;
            throw new ConflictException("You already applied to this position. Check My applications for its status.");
        }
    }

    public async Task UpdateAsync(JobApplication application, CancellationToken cancellationToken)
    {
        context.JobApplications.Update(application);
        await context.SaveChangesAsync(cancellationToken);
    }
}

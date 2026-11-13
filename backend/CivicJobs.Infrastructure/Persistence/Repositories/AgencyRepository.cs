using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence.Repositories;

public sealed class AgencyRepository(CivicJobsDbContext context) : IAgencyRepository
{
    public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Agencies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}

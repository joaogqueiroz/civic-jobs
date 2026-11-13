using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IAgencyRepository
{
    Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

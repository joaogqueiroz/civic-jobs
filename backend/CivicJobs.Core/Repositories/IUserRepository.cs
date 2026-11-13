using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Admins, recruiters and hiring managers of the agency, sorted by name.</summary>
    Task<IReadOnlyList<User>> ListStaffByAgencyAsync(Guid agencyId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}

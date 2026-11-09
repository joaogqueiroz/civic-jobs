using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}

using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(CivicJobsDbContext context) : IUserRepository
{
    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteUniqueConstraintError = 2067;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintError })
        {
            // Two registrations with the same email raced past the service check; the unique index caught the second.
            context.Entry(user).State = EntityState.Detached;
            throw new ConflictException("An account with this email already exists. Log in, or use a different email.");
        }
    }
}

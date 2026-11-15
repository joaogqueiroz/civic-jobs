using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository(CivicJobsDbContext context) : IDepartmentRepository
{
    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteUniqueConstraintError = 2067;

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Department>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken) =>
        await context.Departments
            .AsNoTracking()
            .Where(d => d.AgencyId == agencyId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    // The Name column uses the NOCASE collation, so this comparison ignores case in SQL.
    public Task<bool> ExistsAsync(Guid agencyId, string name, CancellationToken cancellationToken) =>
        context.Departments.AnyAsync(d => d.AgencyId == agencyId && d.Name == name, cancellationToken);

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        context.Departments.Add(department);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintError })
        {
            // Two admins created the same name at once; the unique index caught the second one.
            context.Entry(department).State = EntityState.Detached;
            throw new ConflictException("Your agency already has a department with this name.");
        }
    }
}

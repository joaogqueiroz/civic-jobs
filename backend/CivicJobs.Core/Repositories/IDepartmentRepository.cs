using CivicJobs.Core.Entities;

namespace CivicJobs.Core.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The agency's departments, sorted by name.</summary>
    Task<IReadOnlyList<Department>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken);

    /// <summary>True when the agency already has a department with this name, ignoring case (BR-14).</summary>
    Task<bool> ExistsAsync(Guid agencyId, string name, CancellationToken cancellationToken);

    Task AddAsync(Department department, CancellationToken cancellationToken);
}

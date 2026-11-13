using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;

namespace CivicJobs.Application.Services.Implementations;

/// <summary>Loads the caller's account from the id in the token, for rules that depend on their agency or department.</summary>
internal static class StaffContext
{
    private const string SessionEnded = "Your session has ended. Log in again to continue.";

    /// <summary>The caller's agency. A token whose user is gone, or has no agency, is treated as not authenticated.</summary>
    public static async Task<Guid> GetAgencyIdOfAsync(
        this IUserRepository users,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);

        return user?.AgencyId ?? throw new UnauthorizedException(SessionEnded);
    }

    /// <summary>The caller's department, for recruiters and hiring managers.</summary>
    public static async Task<Department> GetDepartmentOfAsync(
        this IUserRepository users,
        IDepartmentRepository departments,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        var department = user?.DepartmentId is { } departmentId
            ? await departments.GetByIdAsync(departmentId, cancellationToken)
            : null;

        return department ?? throw new UnauthorizedException(SessionEnded);
    }
}

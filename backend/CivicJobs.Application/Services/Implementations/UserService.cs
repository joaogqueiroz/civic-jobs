using CivicJobs.Application.Exceptions;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Repositories;

namespace CivicJobs.Application.Services.Implementations;

public sealed class UserService(
    IUserRepository users,
    IAgencyRepository agencies,
    IDepartmentRepository departments) : IUserService
{
    public async Task<UserViewModel> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        // A valid token for a user that no longer exists is treated as not authenticated (401), as the contract says.
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("Your session has ended. Log in again to continue.");

        var agency = user.AgencyId is { } agencyId ? await agencies.GetByIdAsync(agencyId, cancellationToken) : null;
        var department = user.DepartmentId is { } departmentId
            ? await departments.GetByIdAsync(departmentId, cancellationToken)
            : null;

        return UserViewModel.FromEntity(user, agency, department);
    }
}

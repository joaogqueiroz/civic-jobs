using CivicJobs.Application.Exceptions;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Repositories;

namespace CivicJobs.Application.Services;

public sealed class UserService(IUserRepository users) : IUserService
{
    public async Task<UserViewModel> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        // A valid token for a user that no longer exists is treated as not authenticated (401), as the contract says.
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("The user for this token no longer exists.");

        return UserViewModel.FromEntity(user);
    }
}

using CivicJobs.Application.Exceptions;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Repositories;

namespace CivicJobs.Application.Services;

public sealed class UserService(IUserRepository users) : IUserService
{
    public async Task<UserViewModel> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");

        return UserViewModel.FromEntity(user);
    }
}

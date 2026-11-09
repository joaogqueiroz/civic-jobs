using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services;

public interface IUserService
{
    Task<UserViewModel> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
}

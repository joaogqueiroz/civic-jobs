using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

public interface IUserService
{
    Task<UserViewModel> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
}

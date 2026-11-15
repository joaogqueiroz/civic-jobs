using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

public interface IAuthService
{
    Task<UserViewModel> RegisterAsync(RegisterInputModel input, CancellationToken cancellationToken);

    Task<AuthTokenViewModel> LoginAsync(LoginInputModel input, CancellationToken cancellationToken);
}

using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;

namespace CivicJobs.Application.Abstractions;

public interface ITokenService
{
    AuthTokenViewModel CreateToken(User user);
}

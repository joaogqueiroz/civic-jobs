using CivicJobs.Core.Entities;

namespace CivicJobs.Application.ViewModels;

public sealed record UserViewModel(Guid Id, string Email, string FullName, DateTimeOffset CreatedAt)
{
    public static UserViewModel FromEntity(User user) => new(user.Id, user.Email, user.FullName, user.CreatedAt);
}

using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

public sealed record UserViewModel(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    Guid? AgencyId,
    string? AgencyName,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTimeOffset CreatedAt)
{
    public static UserViewModel FromEntity(User user, Agency? agency = null, Department? department = null) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.AgencyId,
        agency?.Name,
        user.DepartmentId,
        department?.Name,
        user.CreatedAt);
}

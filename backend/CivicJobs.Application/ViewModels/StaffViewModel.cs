using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

/// <summary>A staff account as the agency admin sees it (FR-13).</summary>
public sealed record StaffViewModel(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTimeOffset CreatedAt)
{
    public static StaffViewModel FromEntity(User user, string? departmentName) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.DepartmentId,
        departmentName,
        user.CreatedAt);
}

using CivicJobs.Core.Entities;

namespace CivicJobs.Application.ViewModels;

public sealed record DepartmentViewModel(Guid Id, string Name, DateTimeOffset CreatedAt)
{
    public static DepartmentViewModel FromEntity(Department department) =>
        new(department.Id, department.Name, department.CreatedAt);
}

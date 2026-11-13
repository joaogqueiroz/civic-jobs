using CivicJobs.Core.Enums;

namespace CivicJobs.Application.InputModels;

/// <summary>The role of a staff account and, for recruiters and hiring managers, their department (BR-13).</summary>
public abstract class StaffRoleInputModel
{
    public UserRole Role { get; set; }
    public Guid? DepartmentId { get; set; }
}

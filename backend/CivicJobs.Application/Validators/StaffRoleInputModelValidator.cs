using CivicJobs.Application.InputModels;
using CivicJobs.Core.Enums;
using FluentValidation;

namespace CivicJobs.Application.Validators;

/// <summary>BR-13 as form errors: the same rules the User entity guards, with messages shown under the fields.</summary>
public abstract class StaffRoleInputModelValidator<T> : AbstractValidator<T>
    where T : StaffRoleInputModel
{
    protected StaffRoleInputModelValidator()
    {
        RuleFor(m => m.Role)
            .Must(role => role is UserRole.Admin or UserRole.Recruiter or UserRole.HiringManager)
            .WithMessage("Choose a staff role: admin, recruiter or hiring manager.");

        RuleFor(m => m.DepartmentId)
            .Must(id => id is not null && id != Guid.Empty)
            .When(m => m.Role is UserRole.Recruiter or UserRole.HiringManager)
            .WithMessage("Choose a department for a recruiter or hiring manager.");

        RuleFor(m => m.DepartmentId)
            .Null()
            .When(m => m.Role == UserRole.Admin)
            .WithMessage("An admin belongs to the agency, not to a department.");
    }
}

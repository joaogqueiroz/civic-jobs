using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class ChangeRoleInputModelValidatorTests
{
    private readonly ChangeRoleInputModelValidator _validator = new();

    [Fact]
    [Trait("Requirement", "FR-14")]
    public void Validate_WithHiringManagerAndDepartment_HasNoErrors()
    {
        _validator.TestValidate(new ChangeRoleInputModel { Role = UserRole.HiringManager, DepartmentId = Guid.NewGuid() })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Validate_WithCandidateRole_HasRoleError()
    {
        _validator.TestValidate(new ChangeRoleInputModel { Role = UserRole.Candidate })
            .ShouldHaveValidationErrorFor(m => m.Role);
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Validate_WithRecruiterWithoutDepartment_HasDepartmentError()
    {
        _validator.TestValidate(new ChangeRoleInputModel { Role = UserRole.Recruiter })
            .ShouldHaveValidationErrorFor(m => m.DepartmentId);
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Validate_WithAdminWithDepartment_HasDepartmentError()
    {
        _validator.TestValidate(new ChangeRoleInputModel { Role = UserRole.Admin, DepartmentId = Guid.NewGuid() })
            .ShouldHaveValidationErrorFor(m => m.DepartmentId);
    }
}

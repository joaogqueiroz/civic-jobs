using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Validators;

public class CreateStaffInputModelValidatorTests
{
    private readonly CreateStaffInputModelValidator _validator = new();

    [Theory]
    [Trait("Requirement", "FR-13")]
    [InlineData(UserRole.Recruiter)]
    [InlineData(UserRole.HiringManager)]
    public void Validate_WithDepartmentRoleAndDepartment_HasNoErrors(UserRole role)
    {
        _validator.TestValidate(ValidModel(role, Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Requirement", "FR-13")]
    public void Validate_WithAdminWithoutDepartment_HasNoErrors()
    {
        _validator.TestValidate(ValidModel(UserRole.Admin, null)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Rule", "BR-13")]
    [InlineData(UserRole.Candidate)]
    [InlineData((UserRole)99)]
    public void Validate_WithRoleThatIsNotStaff_HasRoleError(UserRole role)
    {
        _validator.TestValidate(ValidModel(role, null))
            .ShouldHaveValidationErrorFor(m => m.Role)
            .WithErrorMessage("Choose a staff role: admin, recruiter or hiring manager.");
    }

    [Theory]
    [Trait("Rule", "BR-13")]
    [InlineData(UserRole.Recruiter)]
    [InlineData(UserRole.HiringManager)]
    public void Validate_WithDepartmentRoleWithoutDepartment_HasDepartmentError(UserRole role)
    {
        _validator.TestValidate(ValidModel(role, null))
            .ShouldHaveValidationErrorFor(m => m.DepartmentId)
            .WithErrorMessage("Choose a department for a recruiter or hiring manager.");
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Validate_WithAdminWithDepartment_HasDepartmentError()
    {
        _validator.TestValidate(ValidModel(UserRole.Admin, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(m => m.DepartmentId)
            .WithErrorMessage("An admin belongs to the agency, not to a department.");
    }

    [Theory]
    [Trait("Rule", "BR-02")]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public void Validate_WithWeakPassword_HasPasswordError(string password)
    {
        var model = ValidModel(UserRole.Admin, null);
        model.Password = password;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Password);
    }

    [Theory]
    [Trait("Requirement", "FR-13")]
    [InlineData("", "staff@cityofexample.gov")]
    [InlineData("Sam Patel", "not-an-email")]
    public void Validate_WithMissingNameOrInvalidEmail_HasErrors(string fullName, string email)
    {
        var model = ValidModel(UserRole.Admin, null);
        model.FullName = fullName;
        model.Email = email;

        _validator.TestValidate(model).Errors.ShouldNotBeEmpty();
    }

    private static CreateStaffInputModel ValidModel(UserRole role, Guid? departmentId) => new()
    {
        FullName = "Sam Patel",
        Email = "staff@cityofexample.gov",
        Password = "Demo@12345",
        Role = role,
        DepartmentId = departmentId
    };
}

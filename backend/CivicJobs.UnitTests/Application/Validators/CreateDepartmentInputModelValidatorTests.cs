using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class CreateDepartmentInputModelValidatorTests
{
    private readonly CreateDepartmentInputModelValidator _validator = new();

    [Theory]
    [Trait("Rule", "BR-14")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsMissing_HasNameError(string? name)
    {
        _validator.TestValidate(new CreateDepartmentInputModel { Name = name! })
            .ShouldHaveValidationErrorFor(m => m.Name);
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public void Validate_WhenNameIsExactly100Chars_HasNoErrors()
    {
        _validator.TestValidate(new CreateDepartmentInputModel { Name = new string('a', 100) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public void Validate_WhenNameIsLongerThan100Chars_HasNameError()
    {
        _validator.TestValidate(new CreateDepartmentInputModel { Name = new string('a', 101) })
            .ShouldHaveValidationErrorFor(m => m.Name);
    }
}

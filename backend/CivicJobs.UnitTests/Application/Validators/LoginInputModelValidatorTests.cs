using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class LoginInputModelValidatorTests
{
    private readonly LoginInputModelValidator _validator = new();

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void Validate_WithValidModel_HasNoErrors()
    {
        _validator.TestValidate(ValidModel()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Requirement", "FR-02")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenEmailIsMissing_HasEmailError(string? email)
    {
        var model = ValidModel();
        model.Email = email!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Email);
    }

    [Theory]
    [Trait("Requirement", "FR-02")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenPasswordIsMissing_HasPasswordError(string? password)
    {
        var model = ValidModel();
        model.Password = password!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Password);
    }

    private static LoginInputModel ValidModel() => new()
    {
        Email = "recruiter@cityofexample.gov",
        Password = "Demo@12345"
    };
}

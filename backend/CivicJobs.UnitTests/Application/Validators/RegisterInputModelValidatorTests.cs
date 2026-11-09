using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class RegisterInputModelValidatorTests
{
    private readonly RegisterInputModelValidator _validator = new();

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void Validate_WithValidModel_HasNoErrors()
    {
        _validator.TestValidate(ValidModel()).ShouldNotHaveAnyValidationErrors();
    }

    #region Full name: required, up to 100 chars

    [Theory]
    [Trait("Requirement", "FR-01")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenFullNameIsMissing_HasFullNameError(string? fullName)
    {
        var model = ValidModel();
        model.FullName = fullName!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.FullName);
    }

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void Validate_WhenFullNameIsExactly100Chars_HasNoFullNameError()
    {
        var model = ValidModel();
        model.FullName = new string('a', 100);

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.FullName);
    }

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void Validate_WhenFullNameIsLongerThan100Chars_HasFullNameError()
    {
        var model = ValidModel();
        model.FullName = new string('a', 101);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.FullName);
    }

    #endregion

    #region Email: required, valid format

    [Theory]
    [Trait("Requirement", "FR-01")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WhenEmailIsMissingOrInvalid_HasEmailError(string? email)
    {
        var model = ValidModel();
        model.Email = email!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Email);
    }

    #endregion

    #region Password: at least 8 chars, with a letter and a number

    [Theory]
    [Trait("Rule", "BR-02")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc1234")]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    public void Validate_WhenPasswordBreaksPolicy_HasPasswordError(string? password)
    {
        var model = ValidModel();
        model.Password = password!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Password);
    }

    [Fact]
    [Trait("Rule", "BR-02")]
    public void Validate_WhenPasswordHasExactly8CharsWithLetterAndNumber_HasNoPasswordError()
    {
        var model = ValidModel();
        model.Password = "abcdefg1";

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.Password);
    }

    #endregion

    private static RegisterInputModel ValidModel() => new()
    {
        FullName = "Jane Doe",
        Email = "recruiter@cityofexample.gov",
        Password = "Demo@12345"
    };
}

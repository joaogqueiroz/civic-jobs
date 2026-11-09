using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;

namespace CivicJobs.UnitTests.Application.Validators;

public class UpdateJobPostingInputModelValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly UpdateJobPostingInputModelValidator _validator = new(new FakeTimeProvider(Now));

    [Fact]
    [Trait("Requirement", "FR-07")]
    public void Validate_WithValidModel_HasNoErrors()
    {
        _validator.TestValidate(ValidModel()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_WhenTitleIsMissingOrTooShort_HasTitleError(string title)
    {
        var model = ValidModel();
        model.Title = title;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Title);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenDescriptionIsLongerThan4000Chars_HasDescriptionError()
    {
        var model = ValidModel();
        model.Description = new string('a', 4001);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Description);
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Validate_WhenSalaryMaxIsLessThanSalaryMin_HasSalaryMaxError()
    {
        var model = ValidModel();
        model.SalaryMin = 50_000m;
        model.SalaryMax = 49_999m;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.SalaryMax);
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Validate_WhenClosingDateIsInThePast_HasClosingDateError()
    {
        var model = ValidModel();
        model.ClosingDate = Today.AddDays(-1);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.ClosingDate);
    }

    [Fact]
    [Trait("Rule", "BR-10")]
    public void Validate_WhenEmploymentTypeIsNotDefined_HasEmploymentTypeError()
    {
        var model = ValidModel();
        model.EmploymentType = 0;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.EmploymentType);
    }

    private static UpdateJobPostingInputModel ValidModel() => new()
    {
        Title = "Senior Park Ranger",
        Department = "Parks & Recreation",
        Description = "Lead the ranger team.",
        Location = "Central Park District",
        EmploymentType = EmploymentType.FullTime,
        SalaryMin = 60_000m,
        SalaryMax = 75_000m,
        ClosingDate = new DateOnly(2026, 12, 31)
    };
}

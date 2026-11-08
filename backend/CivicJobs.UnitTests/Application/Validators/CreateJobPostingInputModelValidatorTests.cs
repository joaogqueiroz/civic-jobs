using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;

namespace CivicJobs.UnitTests.Application.Validators;

public class CreateJobPostingInputModelValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly CreateJobPostingInputModelValidator _validator = new(new FakeTimeProvider(Now));

    [Fact]
    [Trait("Rule", "BR-03")]
    [Trait("Rule", "BR-04")]
    public void Validate_WithValidModel_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidModel());

        result.ShouldNotHaveAnyValidationErrors();
    }

    // Title: required, 3 to 150 chars

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTitleIsMissing_HasTitleError(string? title)
    {
        var model = ValidModel();
        model.Title = title!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Title);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenTitleIsShorterThan3Chars_HasTitleError()
    {
        var model = ValidModel();
        model.Title = new string('a', 2);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Title);
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(3)]
    [InlineData(150)]
    public void Validate_WhenTitleLengthIsAtTheLimits_HasNoTitleError(int length)
    {
        var model = ValidModel();
        model.Title = new string('a', length);

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.Title);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenTitleIsLongerThan150Chars_HasTitleError()
    {
        var model = ValidModel();
        model.Title = new string('a', 151);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Title);
    }

    // Department: required, up to 100 chars

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDepartmentIsMissing_HasDepartmentError(string? department)
    {
        var model = ValidModel();
        model.Department = department!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Department);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenDepartmentIsExactly100Chars_HasNoDepartmentError()
    {
        var model = ValidModel();
        model.Department = new string('a', 100);

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.Department);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenDepartmentIsLongerThan100Chars_HasDepartmentError()
    {
        var model = ValidModel();
        model.Department = new string('a', 101);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Department);
    }

    // Description: required, up to 4,000 chars

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDescriptionIsMissing_HasDescriptionError(string? description)
    {
        var model = ValidModel();
        model.Description = description!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Description);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenDescriptionIsExactly4000Chars_HasNoDescriptionError()
    {
        var model = ValidModel();
        model.Description = new string('a', 4000);

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.Description);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenDescriptionIsLongerThan4000Chars_HasDescriptionError()
    {
        var model = ValidModel();
        model.Description = new string('a', 4001);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Description);
    }

    // Location: required, up to 100 chars

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenLocationIsMissing_HasLocationError(string? location)
    {
        var model = ValidModel();
        model.Location = location!;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Location);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenLocationIsExactly100Chars_HasNoLocationError()
    {
        var model = ValidModel();
        model.Location = new string('a', 100);

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.Location);
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public void Validate_WhenLocationIsLongerThan100Chars_HasLocationError()
    {
        var model = ValidModel();
        model.Location = new string('a', 101);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.Location);
    }

    // Salary range

    [Theory]
    [Trait("Rule", "BR-04")]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Validate_WhenSalaryMinIsNotPositive_HasSalaryMinError(double salaryMin)
    {
        var model = ValidModel();
        model.SalaryMin = (decimal)salaryMin;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.SalaryMin);
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Validate_WhenSalaryMaxIsLessThanSalaryMin_HasSalaryMaxError()
    {
        var model = ValidModel();
        model.SalaryMin = 50_000m;
        model.SalaryMax = 49_999.99m;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.SalaryMax);
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Validate_WhenSalaryMaxEqualsSalaryMin_HasNoSalaryMaxError()
    {
        var model = ValidModel();
        model.SalaryMin = 50_000m;
        model.SalaryMax = 50_000m;

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.SalaryMax);
    }

    // Closing date

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Validate_WhenClosingDateIsInThePast_HasClosingDateError()
    {
        var model = ValidModel();
        model.ClosingDate = Today.AddDays(-1);

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.ClosingDate);
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Validate_WhenClosingDateIsToday_HasNoClosingDateError()
    {
        var model = ValidModel();
        model.ClosingDate = Today;

        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(m => m.ClosingDate);
    }

    // Employment type

    [Theory]
    [Trait("Rule", "BR-10")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Validate_WhenEmploymentTypeIsNotDefined_HasEmploymentTypeError(int employmentType)
    {
        var model = ValidModel();
        model.EmploymentType = (EmploymentType)employmentType;

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(m => m.EmploymentType);
    }

    private static CreateJobPostingInputModel ValidModel() => new()
    {
        Title = "Park Ranger",
        Department = "Parks & Recreation",
        Description = "Patrol city parks and assist visitors.",
        Location = "Central Park District",
        EmploymentType = EmploymentType.FullTime,
        SalaryMin = 50_000m,
        SalaryMax = 60_000m,
        ClosingDate = new DateOnly(2026, 12, 31)
    };
}

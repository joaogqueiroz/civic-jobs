using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class ApplyInputModelValidatorTests
{
    private readonly ApplyInputModelValidator _validator = new();

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Validate_WithoutCoverLetter_HasNoErrors()
    {
        _validator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid() })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Validate_WithCoverLetterOfExactly2000Chars_HasNoErrors()
    {
        _validator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid(), CoverLetter = new string('a', 2000) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Validate_WithCoverLetterLongerThan2000Chars_HasCoverLetterError()
    {
        _validator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid(), CoverLetter = new string('a', 2001) })
            .ShouldHaveValidationErrorFor(m => m.CoverLetter);
    }

    [Fact]
    [Trait("Requirement", "FR-18")]
    public void Validate_WithoutPosting_HasJobPostingIdError()
    {
        _validator.TestValidate(new ApplyInputModel())
            .ShouldHaveValidationErrorFor(m => m.JobPostingId);
    }
}

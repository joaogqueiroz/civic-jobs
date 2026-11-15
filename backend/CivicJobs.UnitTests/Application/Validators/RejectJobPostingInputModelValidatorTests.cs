using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class RejectJobPostingInputModelValidatorTests
{
    private readonly RejectJobPostingInputModelValidator _validator = new();

    [Theory]
    [Trait("Rule", "BR-17")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenReasonIsMissing_HasReasonError(string? reason)
    {
        _validator.TestValidate(new RejectJobPostingInputModel { Reason = reason! })
            .ShouldHaveValidationErrorFor(m => m.Reason)
            .WithErrorMessage("Tell the recruiter what to change.");
    }

    [Fact]
    [Trait("Rule", "BR-17")]
    public void Validate_WhenReasonIsExactly500Chars_HasNoErrors()
    {
        _validator.TestValidate(new RejectJobPostingInputModel { Reason = new string('a', 500) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-17")]
    public void Validate_WhenReasonIsLongerThan500Chars_HasReasonError()
    {
        _validator.TestValidate(new RejectJobPostingInputModel { Reason = new string('a', 501) })
            .ShouldHaveValidationErrorFor(m => m.Reason);
    }
}

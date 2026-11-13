using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class ApplicationInputModelValidatorTests
{
    private readonly ApplyInputModelValidator _applyValidator = new();
    private readonly ChangeApplicationStatusInputModelValidator _statusValidator = new();

    #region Apply

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Apply_WithoutCoverLetter_HasNoErrors()
    {
        _applyValidator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid() })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Apply_WithCoverLetterOfExactly2000Chars_HasNoErrors()
    {
        _applyValidator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid(), CoverLetter = new string('a', 2000) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Apply_WithCoverLetterLongerThan2000Chars_HasCoverLetterError()
    {
        _applyValidator.TestValidate(new ApplyInputModel { JobPostingId = Guid.NewGuid(), CoverLetter = new string('a', 2001) })
            .ShouldHaveValidationErrorFor(m => m.CoverLetter);
    }

    [Fact]
    [Trait("Requirement", "FR-18")]
    public void Apply_WithoutPosting_HasJobPostingIdError()
    {
        _applyValidator.TestValidate(new ApplyInputModel())
            .ShouldHaveValidationErrorFor(m => m.JobPostingId);
    }

    #endregion

    #region Change status

    [Theory]
    [Trait("Requirement", "FR-21")]
    [InlineData(JobApplicationStatus.UnderReview)]
    [InlineData(JobApplicationStatus.Rejected)]
    [InlineData(JobApplicationStatus.Hired)]
    public void ChangeStatus_ToADecisionStatus_HasNoErrors(JobApplicationStatus status)
    {
        _statusValidator.TestValidate(new ChangeApplicationStatusInputModel { Status = status })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Rule", "BR-19")]
    [InlineData(JobApplicationStatus.Submitted)]
    [InlineData((JobApplicationStatus)99)]
    public void ChangeStatus_ToSubmittedOrUnknown_HasStatusError(JobApplicationStatus status)
    {
        _statusValidator.TestValidate(new ChangeApplicationStatusInputModel { Status = status })
            .ShouldHaveValidationErrorFor(m => m.Status)
            .WithErrorMessage("Choose Under review, Rejected or Hired.");
    }

    #endregion
}

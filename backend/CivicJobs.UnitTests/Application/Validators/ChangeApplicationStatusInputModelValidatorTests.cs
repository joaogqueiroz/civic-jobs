using CivicJobs.Application.InputModels;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Enums;
using FluentValidation.TestHelper;

namespace CivicJobs.UnitTests.Application.Validators;

public class ChangeApplicationStatusInputModelValidatorTests
{
    private readonly ChangeApplicationStatusInputModelValidator _validator = new();

    [Theory]
    [Trait("Requirement", "FR-21")]
    [InlineData(JobApplicationStatus.UnderReview)]
    [InlineData(JobApplicationStatus.Rejected)]
    [InlineData(JobApplicationStatus.Hired)]
    public void Validate_ToADecisionStatus_HasNoErrors(JobApplicationStatus status)
    {
        _validator.TestValidate(new ChangeApplicationStatusInputModel { Status = status })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Rule", "BR-19")]
    [InlineData(JobApplicationStatus.Submitted)]
    [InlineData((JobApplicationStatus)99)]
    public void Validate_ToSubmittedOrUnknown_HasStatusError(JobApplicationStatus status)
    {
        _validator.TestValidate(new ChangeApplicationStatusInputModel { Status = status })
            .ShouldHaveValidationErrorFor(m => m.Status)
            .WithErrorMessage("Choose Under review, Rejected or Hired.");
    }
}

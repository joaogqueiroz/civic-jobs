using CivicJobs.Application.InputModels;
using CivicJobs.Core.Enums;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class ChangeApplicationStatusInputModelValidator : AbstractValidator<ChangeApplicationStatusInputModel>
{
    public ChangeApplicationStatusInputModelValidator()
    {
        // Submitted is where every application starts, never a decision.
        RuleFor(m => m.Status)
            .Must(status => status is JobApplicationStatus.UnderReview or JobApplicationStatus.Rejected or JobApplicationStatus.Hired)
            .WithMessage("Choose Under review, Rejected or Hired.");
    }
}

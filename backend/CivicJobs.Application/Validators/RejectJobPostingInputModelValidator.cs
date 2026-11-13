using CivicJobs.Application.InputModels;
using CivicJobs.Core.Entities;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class RejectJobPostingInputModelValidator : AbstractValidator<RejectJobPostingInputModel>
{
    public RejectJobPostingInputModelValidator()
    {
        RuleFor(m => m.Reason)
            .NotEmpty()
            .WithMessage("Tell the recruiter what to change.")
            .MaximumLength(JobPosting.RejectionReasonMaxLength);
    }
}

using CivicJobs.Application.InputModels;
using CivicJobs.Core.Entities;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class ApplyInputModelValidator : AbstractValidator<ApplyInputModel>
{
    public ApplyInputModelValidator()
    {
        RuleFor(m => m.JobPostingId).NotEmpty();

        RuleFor(m => m.CoverLetter)
            .MaximumLength(JobApplication.CoverLetterMaxLength)
            .WithMessage($"Keep the cover letter under {JobApplication.CoverLetterMaxLength} characters.");
    }
}

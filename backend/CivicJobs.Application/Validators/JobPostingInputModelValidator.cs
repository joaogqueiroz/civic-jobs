using CivicJobs.Application.InputModels;
using CivicJobs.Core.Entities;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public abstract class JobPostingInputModelValidator<T> : AbstractValidator<T>
    where T : JobPostingInputModel
{
    protected JobPostingInputModelValidator(TimeProvider timeProvider)
    {
        RuleFor(m => m.Title)
            .NotEmpty()
            .MinimumLength(JobPosting.TitleMinLength)
            .MaximumLength(JobPosting.TitleMaxLength);

        RuleFor(m => m.Department)
            .NotEmpty()
            .MaximumLength(JobPosting.DepartmentMaxLength);

        RuleFor(m => m.Description)
            .NotEmpty()
            .MaximumLength(JobPosting.DescriptionMaxLength);

        RuleFor(m => m.Location)
            .NotEmpty()
            .MaximumLength(JobPosting.LocationMaxLength);

        RuleFor(m => m.EmploymentType)
            .IsInEnum();

        RuleFor(m => m.SalaryMin)
            .GreaterThan(0);

        RuleFor(m => m.SalaryMax)
            .GreaterThanOrEqualTo(m => m.SalaryMin);

        RuleFor(m => m.ClosingDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("'Closing Date' can't be in the past.");
    }
}

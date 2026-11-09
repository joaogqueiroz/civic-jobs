using CivicJobs.Application.InputModels;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public abstract class JobPostingInputModelValidator<T> : AbstractValidator<T>
    where T : JobPostingInputModel
{
    protected JobPostingInputModelValidator(TimeProvider timeProvider)
    {
        RuleFor(m => m.Title)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(150);

        RuleFor(m => m.Department)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(m => m.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(m => m.Location)
            .NotEmpty()
            .MaximumLength(100);

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

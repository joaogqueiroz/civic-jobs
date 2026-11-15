using System.Globalization;
using CivicJobs.Application.InputModels;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public abstract class JobPostingInputModelValidator<T> : AbstractValidator<T>
    where T : JobPostingInputModel
{
    // Salaries are in USD (the UI labels them that way), whatever culture the server runs in.
    private static readonly CultureInfo UsDollars = CultureInfo.GetCultureInfo("en-US");

    protected JobPostingInputModelValidator(TimeProvider timeProvider)
    {
        RuleFor(m => m.Title)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(150);

        RuleFor(m => m.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(m => m.Location)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(m => m.EmploymentType)
            .IsInEnum();

        // These messages are shown under the form fields, so they say how to fix the value, in plain words.
        RuleFor(m => m.SalaryMin)
            .GreaterThan(0)
            .WithMessage("Enter a minimum salary greater than zero.");

        RuleFor(m => m.SalaryMax)
            .GreaterThanOrEqualTo(m => m.SalaryMin)
            .WithMessage(m => $"Enter a maximum salary of at least {m.SalaryMin.ToString("C0", UsDollars)}, the minimum salary.");

        RuleFor(m => m.ClosingDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Choose a closing date of today or later.");
    }
}

using CivicJobs.Application.InputModels;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class RegisterInputModelValidator : AbstractValidator<RegisterInputModel>
{
    public RegisterInputModelValidator()
    {
        RuleFor(m => m.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(m => m.Email)
            .NotEmpty()
            .EmailAddress();

        // The upper limit caps the work done by password hashing, so very long passwords can't slow the server down.
        RuleFor(m => m.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Za-z]").WithMessage("'Password' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'Password' must contain at least one number.");
    }
}

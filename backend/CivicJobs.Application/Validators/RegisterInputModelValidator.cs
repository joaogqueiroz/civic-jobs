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

        RuleFor(m => m.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("'Password' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'Password' must contain at least one number.");
    }
}

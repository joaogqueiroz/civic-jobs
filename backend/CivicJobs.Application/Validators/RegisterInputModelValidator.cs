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
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(m => m.Password).StrongPassword();
    }
}

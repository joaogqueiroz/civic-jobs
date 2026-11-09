using CivicJobs.Application.InputModels;
using CivicJobs.Core.Entities;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class RegisterInputModelValidator : AbstractValidator<RegisterInputModel>
{
    // Caps the work done by password hashing, so very long passwords can't be used to slow the server down.
    public const int PasswordMaxLength = 128;

    public RegisterInputModelValidator()
    {
        RuleFor(m => m.FullName)
            .NotEmpty()
            .MaximumLength(User.FullNameMaxLength);

        RuleFor(m => m.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(m => m.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(PasswordMaxLength)
            .Matches("[A-Za-z]").WithMessage("'Password' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'Password' must contain at least one number.");
    }
}

using CivicJobs.Application.InputModels;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class LoginInputModelValidator : AbstractValidator<LoginInputModel>
{
    public LoginInputModelValidator()
    {
        RuleFor(m => m.Email).NotEmpty();
        RuleFor(m => m.Password)
            .NotEmpty()
            .MaximumLength(RegisterInputModelValidator.PasswordMaxLength);
    }
}

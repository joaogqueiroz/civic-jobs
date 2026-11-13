using FluentValidation;

namespace CivicJobs.Application.Validators;

/// <summary>BR-02, shared by public registration and staff accounts created by an admin.</summary>
public static class PasswordRules
{
    // The upper limit caps the work done by password hashing, so very long passwords can't slow the server down.
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Za-z]").WithMessage("'Password' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'Password' must contain at least one number.");
}

using CivicJobs.Application.InputModels;
using FluentValidation;

namespace CivicJobs.Application.Validators;

public sealed class CreateDepartmentInputModelValidator : AbstractValidator<CreateDepartmentInputModel>
{
    public CreateDepartmentInputModelValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

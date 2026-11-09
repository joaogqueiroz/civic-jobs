using CivicJobs.Application.InputModels;

namespace CivicJobs.Application.Validators;

public sealed class UpdateJobPostingInputModelValidator(TimeProvider timeProvider)
    : JobPostingInputModelValidator<UpdateJobPostingInputModel>(timeProvider);

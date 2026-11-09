using CivicJobs.Application.InputModels;

namespace CivicJobs.Application.Validators;

public sealed class CreateJobPostingInputModelValidator(TimeProvider timeProvider)
    : JobPostingInputModelValidator<CreateJobPostingInputModel>(timeProvider);

using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services;

public sealed class JobPostingService(
    IValidator<CreateJobPostingInputModel> validator,
    IJobPostingRepository repository,
    TimeProvider timeProvider) : IJobPostingService
{
    public async Task<JobPostingViewModel> CreateAsync(
        Guid userId,
        CreateJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(input, cancellationToken);

        var posting = JobPosting.Create(
            createdByUserId: userId,
            title: input.Title,
            department: input.Department,
            description: input.Description,
            location: input.Location,
            employmentType: input.EmploymentType,
            salaryMin: input.SalaryMin,
            salaryMax: input.SalaryMax,
            closingDate: input.ClosingDate,
            createdAt: timeProvider.GetUtcNow());

        await repository.AddAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }
}

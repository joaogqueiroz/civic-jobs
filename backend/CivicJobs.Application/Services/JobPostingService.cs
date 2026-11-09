using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using FluentValidation;

namespace CivicJobs.Application.Services;

public sealed class JobPostingService(
    IValidator<CreateJobPostingInputModel> validator,
    TimeProvider timeProvider) : IJobPostingService
{
    public Task<JobPostingViewModel> CreateAsync(
        Guid userId,
        CreateJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
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

        return Task.FromResult(JobPostingViewModel.FromEntity(posting));
    }
}

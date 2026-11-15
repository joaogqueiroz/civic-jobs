using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class JobPostingService(
    IValidator<CreateJobPostingInputModel> createValidator,
    IValidator<UpdateJobPostingInputModel> updateValidator,
    IJobPostingRepository repository,
    IUserRepository users,
    IDepartmentRepository departments,
    TimeProvider timeProvider) : IJobPostingService
{
    public async Task<JobPostingViewModel> CreateAsync(
        Guid userId,
        CreateJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(input, cancellationToken);

        // The department comes from the recruiter's account, never from the request (BR-13).
        var department = await users.GetDepartmentOfAsync(departments, userId, cancellationToken);

        var posting = JobPosting.Create(
            createdByUserId: userId,
            departmentId: department.Id,
            department: department.Name,
            title: input.Title,
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

    public async Task<JobPostingViewModel> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await GetOwnedAsync(userId, id, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    public async Task<IReadOnlyList<JobPostingViewModel>> ListAsync(
        Guid userId,
        JobPostingStatus? status,
        CancellationToken cancellationToken)
    {
        var postings = await repository.ListByOwnerAsync(userId, status, cancellationToken);

        return postings.Select(JobPostingViewModel.FromEntity).ToList();
    }

    public async Task<JobPostingViewModel> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
        // Order of checks: 404 (missing or not owned), 409 (Closed or PendingApproval), 400 (invalid input), then the entity rules.
        var posting = await GetOwnedAsync(userId, id, cancellationToken);
        posting.EnsureCanBeEdited();

        await updateValidator.ValidateAndThrowAsync(input, cancellationToken);

        posting.Update(
            title: input.Title,
            description: input.Description,
            location: input.Location,
            employmentType: input.EmploymentType,
            salaryMin: input.SalaryMin,
            salaryMax: input.SalaryMax,
            closingDate: input.ClosingDate,
            updatedAt: timeProvider.GetUtcNow());

        await repository.UpdateAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await GetOwnedAsync(userId, id, cancellationToken);

        posting.EnsureCanBeDeleted();

        await repository.DeleteAsync(posting, cancellationToken);
    }

    public async Task<JobPostingViewModel> SubmitAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await GetOwnedAsync(userId, id, cancellationToken);

        posting.SubmitForApproval(timeProvider.GetUtcNow());

        await repository.UpdateAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    public async Task<JobPostingViewModel> CloseAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await GetOwnedAsync(userId, id, cancellationToken);

        posting.Close(timeProvider.GetUtcNow());

        await repository.UpdateAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    public async Task<IReadOnlyList<PublicJobPostingViewModel>> ListPublicAsync(CancellationToken cancellationToken)
    {
        // Candidates only see postings that are still valid: an Open posting past its closing date is hidden.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var postings = await repository.ListOpenAsync(today, cancellationToken);

        return postings.Select(PublicJobPostingViewModel.FromEntity).ToList();
    }

    private async Task<JobPosting> GetOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await repository.GetByIdAsync(id, cancellationToken);

        // A posting owned by another user is reported as not found, so the API doesn't reveal that the id exists (BR-08).
        if (posting is null || posting.CreatedByUserId != userId)
        {
            throw new NotFoundException("We couldn't find that job posting. It may have been deleted.");
        }

        return posting;
    }
}

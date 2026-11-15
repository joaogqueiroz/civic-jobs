using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class DepartmentJobPostingService(
    IValidator<RejectJobPostingInputModel> rejectValidator,
    IJobPostingRepository postings,
    IUserRepository users,
    IDepartmentRepository departments,
    TimeProvider timeProvider) : IDepartmentJobPostingService
{
    public async Task<IReadOnlyList<JobPostingViewModel>> ListAsync(
        Guid managerId,
        JobPostingStatus? status,
        CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentOfAsync(departments, managerId, cancellationToken);
        var list = await postings.ListByDepartmentAsync(department.Id, status, cancellationToken);

        return list.Select(JobPostingViewModel.FromEntity).ToList();
    }

    public async Task<JobPostingViewModel> GetByIdAsync(Guid managerId, Guid id, CancellationToken cancellationToken) =>
        JobPostingViewModel.FromEntity(await GetDepartmentPostingAsync(managerId, id, cancellationToken));

    public async Task<JobPostingViewModel> ApproveAsync(Guid managerId, Guid id, CancellationToken cancellationToken)
    {
        var posting = await GetDepartmentPostingAsync(managerId, id, cancellationToken);

        posting.Approve(timeProvider.GetUtcNow());
        await postings.UpdateAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    public async Task<JobPostingViewModel> RejectAsync(
        Guid managerId,
        Guid id,
        RejectJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
        // Order of checks: 404 (not in the manager's department), 400 (no reason), 409 (not waiting for approval).
        var posting = await GetDepartmentPostingAsync(managerId, id, cancellationToken);

        await rejectValidator.ValidateAndThrowAsync(input, cancellationToken);

        posting.Reject(input.Reason, timeProvider.GetUtcNow());
        await postings.UpdateAsync(posting, cancellationToken);

        return JobPostingViewModel.FromEntity(posting);
    }

    private async Task<JobPosting> GetDepartmentPostingAsync(Guid managerId, Guid id, CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentOfAsync(departments, managerId, cancellationToken);

        return await postings.GetDepartmentPostingAsync(department, id, cancellationToken);
    }
}

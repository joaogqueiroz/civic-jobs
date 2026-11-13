using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class DepartmentApplicationService(
    IValidator<ChangeApplicationStatusInputModel> statusValidator,
    IJobApplicationRepository applications,
    IJobPostingRepository postings,
    IUserRepository users,
    IDepartmentRepository departments,
    IEmailSender emailSender,
    TimeProvider timeProvider) : IDepartmentApplicationService
{
    public async Task<IReadOnlyList<PostingApplicationViewModel>> ListByPostingAsync(
        Guid managerId,
        Guid jobPostingId,
        CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentOfAsync(departments, managerId, cancellationToken);
        var posting = await postings.GetDepartmentPostingAsync(department, jobPostingId, cancellationToken);

        var list = await applications.ListByPostingAsync(posting.Id, cancellationToken);
        var candidateIds = list.Select(a => a.CandidateId).Distinct().ToList();
        var candidates = (await users.ListByIdsAsync(candidateIds, cancellationToken)).ToDictionary(u => u.Id);

        return list
            .Select(a => PostingApplicationViewModel.FromEntity(a, candidates.GetValueOrDefault(a.CandidateId)))
            .ToList();
    }

    public async Task<PostingApplicationViewModel> ChangeStatusAsync(
        Guid managerId,
        Guid applicationId,
        ChangeApplicationStatusInputModel input,
        CancellationToken cancellationToken)
    {
        // Order of checks: 404 (not in the manager's department), 400 (invalid status), 409 (transition not allowed).
        var department = await users.GetDepartmentOfAsync(departments, managerId, cancellationToken);
        var application = await applications.GetByIdAsync(applicationId, cancellationToken);
        var posting = application is null ? null : await postings.GetByIdAsync(application.JobPostingId, cancellationToken);

        if (application is null || posting is null || posting.DepartmentId != department.Id)
        {
            throw new NotFoundException("We couldn't find that application in your department.");
        }

        await statusValidator.ValidateAndThrowAsync(input, cancellationToken);

        application.ChangeStatus(input.Status, timeProvider.GetUtcNow());
        await applications.UpdateAsync(application, cancellationToken);

        var candidate = await users.GetByIdAsync(application.CandidateId, cancellationToken);

        if (candidate is not null)
        {
            await emailSender.SendStatusChangedAsync(candidate, posting, application.Status, cancellationToken);
        }

        return PostingApplicationViewModel.FromEntity(application, candidate);
    }
}

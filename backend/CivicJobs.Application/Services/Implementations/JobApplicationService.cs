using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class JobApplicationService(
    IValidator<ApplyInputModel> applyValidator,
    IJobApplicationRepository applications,
    IJobPostingRepository postings,
    TimeProvider timeProvider) : IJobApplicationService
{
    public async Task<CandidateApplicationViewModel> ApplyAsync(
        Guid candidateId,
        ApplyInputModel input,
        CancellationToken cancellationToken)
    {
        // Order of checks: 400 (invalid input), 404 (not a public posting), 409 (already applied), 409 (closed).
        await applyValidator.ValidateAndThrowAsync(input, cancellationToken);

        var posting = await postings.GetByIdAsync(input.JobPostingId, cancellationToken);

        // Drafts and postings waiting for approval aren't public, so candidates can't tell they exist (BR-18).
        if (posting is null || posting.Status is JobPostingStatus.Draft or JobPostingStatus.PendingApproval)
        {
            throw new NotFoundException("We couldn't find that position. It may have been removed.");
        }

        if (await applications.ExistsAsync(posting.Id, candidateId, cancellationToken))
        {
            throw new ConflictException("You already applied to this position. Check My applications for its status.");
        }

        var application = JobApplication.Submit(posting, candidateId, input.CoverLetter, timeProvider.GetUtcNow());
        await applications.AddAsync(application, cancellationToken);

        return CandidateApplicationViewModel.FromEntity(application, posting);
    }

    public async Task<IReadOnlyList<CandidateApplicationViewModel>> ListMineAsync(
        Guid candidateId,
        CancellationToken cancellationToken)
    {
        var mine = await applications.ListByCandidateAsync(candidateId, cancellationToken);
        var postingIds = mine.Select(a => a.JobPostingId).Distinct().ToList();
        var byId = (await postings.ListByIdsAsync(postingIds, cancellationToken)).ToDictionary(p => p.Id);

        // Applications only exist for postings that were Open, and published postings are never deleted (BR-07).
        return mine
            .Where(a => byId.ContainsKey(a.JobPostingId))
            .Select(a => CandidateApplicationViewModel.FromEntity(a, byId[a.JobPostingId]))
            .ToList();
    }
}

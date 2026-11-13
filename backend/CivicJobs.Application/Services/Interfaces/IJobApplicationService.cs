using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;

namespace CivicJobs.Application.Services.Interfaces;

/// <summary>The candidate's side of applications (FR-18, FR-19).</summary>
public interface IJobApplicationService
{
    Task<CandidateApplicationViewModel> ApplyAsync(Guid candidateId, ApplyInputModel input, CancellationToken cancellationToken);

    Task<IReadOnlyList<CandidateApplicationViewModel>> ListMineAsync(Guid candidateId, CancellationToken cancellationToken);
}

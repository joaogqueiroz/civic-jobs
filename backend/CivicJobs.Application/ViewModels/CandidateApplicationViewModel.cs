using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

/// <summary>An application as the candidate sees it in My applications (FR-19).</summary>
public sealed record CandidateApplicationViewModel(
    Guid Id,
    Guid JobPostingId,
    string JobTitle,
    string Department,
    string Location,
    JobApplicationStatus Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset UpdatedAt)
{
    public static CandidateApplicationViewModel FromEntity(JobApplication application, JobPosting posting) => new(
        application.Id,
        posting.Id,
        posting.Title,
        posting.Department,
        posting.Location,
        application.Status,
        application.CreatedAt,
        application.UpdatedAt);
}

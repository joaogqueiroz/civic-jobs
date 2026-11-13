using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

/// <summary>An application as the hiring manager sees it on the posting (FR-20).</summary>
public sealed record PostingApplicationViewModel(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    string? CoverLetter,
    JobApplicationStatus Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset UpdatedAt)
{
    public static PostingApplicationViewModel FromEntity(JobApplication application, User? candidate) => new(
        application.Id,
        application.CandidateId,
        candidate?.FullName ?? "Deleted account",
        candidate?.Email ?? string.Empty,
        application.CoverLetter,
        application.Status,
        application.CreatedAt,
        application.UpdatedAt);
}

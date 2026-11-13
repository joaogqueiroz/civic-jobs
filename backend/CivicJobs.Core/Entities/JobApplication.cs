using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class JobApplication
{
    public const int CoverLetterMaxLength = 2000;

    public Guid Id { get; private set; }
    public Guid JobPostingId { get; private set; }
    public Guid CandidateId { get; private set; }
    public string? CoverLetter { get; private set; }
    public JobApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private JobApplication()
    {
    }

    /// <summary>
    /// A candidate applies to an Open posting that closes today or later (BR-18). An Open posting past its
    /// closing date is rejected here too, because the hourly close (BR-11) may not have run yet.
    /// </summary>
    public static JobApplication Submit(JobPosting posting, Guid candidateId, string? coverLetter, DateTimeOffset submittedAt)
    {
        if (candidateId == Guid.Empty)
        {
            throw new DomainValidationException("CandidateId is required.");
        }

        if (posting.Status != JobPostingStatus.Open
            || posting.ClosingDate < DateOnly.FromDateTime(submittedAt.UtcDateTime))
        {
            throw new InvalidStatusTransitionException("This position is no longer taking applications.");
        }

        var trimmed = string.IsNullOrWhiteSpace(coverLetter) ? null : coverLetter.Trim();

        if (trimmed?.Length > CoverLetterMaxLength)
        {
            throw new DomainValidationException($"Keep the cover letter under {CoverLetterMaxLength} characters.");
        }

        return new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostingId = posting.Id,
            CandidateId = candidateId,
            CoverLetter = trimmed,
            Status = JobApplicationStatus.Submitted,
            CreatedAt = submittedAt,
            UpdatedAt = submittedAt
        };
    }

    /// <summary>Submitted -> UnderReview -> Hired or Rejected, or Submitted -> Rejected; Hired and Rejected are final (BR-19).</summary>
    public void ChangeStatus(JobApplicationStatus status, DateTimeOffset changedAt)
    {
        if (Status is JobApplicationStatus.Hired or JobApplicationStatus.Rejected)
        {
            throw new InvalidStatusTransitionException("This application already has a final decision.");
        }

        var allowed = (Status, status) switch
        {
            (JobApplicationStatus.Submitted, JobApplicationStatus.UnderReview) => true,
            (JobApplicationStatus.Submitted, JobApplicationStatus.Rejected) => true,
            (JobApplicationStatus.UnderReview, JobApplicationStatus.Hired) => true,
            (JobApplicationStatus.UnderReview, JobApplicationStatus.Rejected) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidStatusTransitionException(Status == JobApplicationStatus.Submitted && status == JobApplicationStatus.Hired
                ? "Review the application before hiring the candidate."
                : $"An application that is {Status} can't move to {status}.");
        }

        Status = status;
        UpdatedAt = changedAt;
    }
}

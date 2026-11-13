using CivicJobs.Application.Abstractions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.Services.Implementations;

/// <summary>The emails a candidate gets about an application (FR-23), worded in one place.</summary>
internal static class CandidateNotifications
{
    public static Task SendApplicationReceivedAsync(
        this IEmailSender emailSender,
        User candidate,
        JobPosting posting,
        CancellationToken cancellationToken) =>
        emailSender.SendAsync(
            candidate.Email,
            $"We received your application for {posting.Title}",
            $"""
            Hello {candidate.FullName},

            Thank you for applying to {posting.Title} at {posting.Department}. The hiring manager will review it,
            and we'll email you when its status changes. You can also follow it in My applications.
            """,
            cancellationToken);

    public static Task SendStatusChangedAsync(
        this IEmailSender emailSender,
        User candidate,
        JobPosting posting,
        JobApplicationStatus status,
        CancellationToken cancellationToken)
    {
        var label = status switch
        {
            JobApplicationStatus.UnderReview => "Under review",
            _ => status.ToString()
        };

        return emailSender.SendAsync(
            candidate.Email,
            $"Your application for {posting.Title}: {label}",
            $"""
            Hello {candidate.FullName},

            Your application for {posting.Title} at {posting.Department} is now: {label}.
            """,
            cancellationToken);
    }
}

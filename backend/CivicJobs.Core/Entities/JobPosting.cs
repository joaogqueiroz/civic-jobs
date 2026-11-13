using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class JobPosting
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }

    /// <summary>The department name when the posting was created, kept as part of the public record.</summary>
    public string Department { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public EmploymentType EmploymentType { get; private set; }
    public decimal SalaryMin { get; private set; }
    public decimal SalaryMax { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public JobPostingStatus Status { get; private set; }

    /// <summary>Why the hiring manager sent the posting back to Draft; cleared when it is submitted again (BR-17).</summary>
    public string? RejectionReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private JobPosting()
    {
    }

    public static JobPosting Create(
        Guid createdByUserId,
        Guid departmentId,
        string department,
        string title,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax,
        DateOnly closingDate,
        DateTimeOffset createdAt)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainValidationException("CreatedByUserId is required.");
        }

        if (departmentId == Guid.Empty)
        {
            throw new DomainValidationException("DepartmentId is required.");
        }

        Guard.Required(department, nameof(Department));
        ValidateDetails(title, description, location, employmentType, salaryMin, salaryMax);
        EnsureClosingDateIsNotInThePast(closingDate, createdAt);

        return new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = title,
            DepartmentId = departmentId,
            Department = department,
            Description = description,
            Location = location,
            EmploymentType = employmentType,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            ClosingDate = closingDate,
            Status = JobPostingStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Update(
        string title,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax,
        DateOnly closingDate,
        DateTimeOffset updatedAt)
    {
        EnsureCanBeEdited();

        ValidateDetails(title, description, location, employmentType, salaryMin, salaryMax);

        if (Status == JobPostingStatus.Open)
        {
            EnsureClosingDateIsAfterToday(closingDate, updatedAt);
        }
        else
        {
            EnsureClosingDateIsNotInThePast(closingDate, updatedAt);
        }

        Title = title;
        Description = description;
        Location = location;
        EmploymentType = employmentType;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        ClosingDate = closingDate;
        UpdatedAt = updatedAt;
    }

    public const int RejectionReasonMaxLength = 500;

    public void EnsureCanBeEdited()
    {
        switch (Status)
        {
            case JobPostingStatus.Closed:
                throw new InvalidStatusTransitionException("This posting is closed, so it can't be edited.");
            case JobPostingStatus.PendingApproval:
                throw new InvalidStatusTransitionException("This posting is waiting for approval, so it can't be edited.");
        }
    }

    public void SubmitForApproval(DateTimeOffset submittedAt)
    {
        if (Status != JobPostingStatus.Draft)
        {
            throw new InvalidStatusTransitionException(Status switch
            {
                JobPostingStatus.PendingApproval => "This posting is already waiting for approval.",
                JobPostingStatus.Open => "This posting is already published.",
                _ => "This posting is closed and can't be published again."
            });
        }

        EnsureClosingDateIsAfterToday(ClosingDate, submittedAt);

        Status = JobPostingStatus.PendingApproval;
        RejectionReason = null;
        UpdatedAt = submittedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        EnsurePendingApproval();

        // Checked again: the closing date may have passed while the posting waited for approval (BR-05).
        EnsureClosingDateIsAfterToday(ClosingDate, approvedAt);

        Status = JobPostingStatus.Open;
        UpdatedAt = approvedAt;
    }

    public void Reject(string reason, DateTimeOffset rejectedAt)
    {
        EnsurePendingApproval();
        Guard.Required(reason, nameof(RejectionReason));

        var trimmed = reason.Trim();

        if (trimmed.Length > RejectionReasonMaxLength)
        {
            throw new DomainValidationException($"Keep the reason under {RejectionReasonMaxLength} characters.");
        }

        Status = JobPostingStatus.Draft;
        RejectionReason = trimmed;
        UpdatedAt = rejectedAt;
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status != JobPostingStatus.Open)
        {
            throw new InvalidStatusTransitionException(Status == JobPostingStatus.Closed
                ? "This posting is already closed."
                : "Only open postings can be closed.");
        }

        Status = JobPostingStatus.Closed;
        UpdatedAt = closedAt;
    }

    public void EnsureCanBeDeleted()
    {
        if (Status != JobPostingStatus.Draft)
        {
            throw new InvalidStatusTransitionException("Only drafts can be deleted. Published postings are kept as a public record.");
        }
    }

    private void EnsurePendingApproval()
    {
        if (Status != JobPostingStatus.PendingApproval)
        {
            throw new InvalidStatusTransitionException("Only postings waiting for approval can be approved or rejected.");
        }
    }

    private static void ValidateDetails(
        string title,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax)
    {
        Guard.Required(title, nameof(Title));
        Guard.Required(description, nameof(Description));
        Guard.Required(location, nameof(Location));

        if (!Enum.IsDefined(employmentType))
        {
            throw new DomainValidationException("Choose an employment type: full time, part time or temporary.");
        }

        if (salaryMin <= 0)
        {
            throw new DomainValidationException("Enter a minimum salary greater than zero.");
        }

        if (salaryMax < salaryMin)
        {
            throw new DomainValidationException("The maximum salary can't be lower than the minimum salary.");
        }
    }

    private static void EnsureClosingDateIsNotInThePast(DateOnly closingDate, DateTimeOffset now)
    {
        if (closingDate < ToDate(now))
        {
            throw new DomainValidationException("Choose a closing date of today or later.");
        }
    }

    private static void EnsureClosingDateIsAfterToday(DateOnly closingDate, DateTimeOffset now)
    {
        if (closingDate <= ToDate(now))
        {
            throw new InvalidStatusTransitionException("Choose a closing date after today. Open postings must close in the future.");
        }
    }

    private static DateOnly ToDate(DateTimeOffset value) => DateOnly.FromDateTime(value.UtcDateTime);
}

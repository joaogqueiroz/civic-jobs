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

    public void EnsureCanBeEdited()
    {
        if (Status == JobPostingStatus.Closed)
        {
            throw new InvalidStatusTransitionException("This posting is closed, so it can't be edited.");
        }
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (Status != JobPostingStatus.Draft)
        {
            throw new InvalidStatusTransitionException(Status == JobPostingStatus.Open
                ? "This posting is already published."
                : "This posting is closed and can't be published again.");
        }

        EnsureClosingDateIsAfterToday(ClosingDate, publishedAt);

        Status = JobPostingStatus.Open;
        UpdatedAt = publishedAt;
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status != JobPostingStatus.Open)
        {
            throw new InvalidStatusTransitionException(Status == JobPostingStatus.Draft
                ? "Publish this draft before closing it."
                : "This posting is already closed.");
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

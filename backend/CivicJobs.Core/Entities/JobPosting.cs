using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class JobPosting
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
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
        string title,
        string department,
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

        ValidateDetails(title, department, description, location, employmentType, salaryMin, salaryMax);
        EnsureClosingDateIsNotInThePast(closingDate, createdAt);

        return new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = title,
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
        string department,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax,
        DateOnly closingDate,
        DateTimeOffset updatedAt)
    {
        EnsureCanBeEdited();

        ValidateDetails(title, department, description, location, employmentType, salaryMin, salaryMax);

        if (Status == JobPostingStatus.Open)
        {
            EnsureClosingDateIsAfterToday(closingDate, updatedAt);
        }
        else
        {
            EnsureClosingDateIsNotInThePast(closingDate, updatedAt);
        }

        Title = title;
        Department = department;
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
            throw new InvalidStatusTransitionException("A closed job posting can't be edited.");
        }
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (Status != JobPostingStatus.Draft)
        {
            throw new InvalidStatusTransitionException($"Only a Draft job posting can be published. Current status: {Status}.");
        }

        EnsureClosingDateIsAfterToday(ClosingDate, publishedAt);

        Status = JobPostingStatus.Open;
        UpdatedAt = publishedAt;
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status != JobPostingStatus.Open)
        {
            throw new InvalidStatusTransitionException($"Only an Open job posting can be closed. Current status: {Status}.");
        }

        Status = JobPostingStatus.Closed;
        UpdatedAt = closedAt;
    }

    public void EnsureCanBeDeleted()
    {
        if (Status != JobPostingStatus.Draft)
        {
            throw new InvalidStatusTransitionException("Only a Draft job posting can be deleted. Published postings are kept as records.");
        }
    }

    private static void ValidateDetails(
        string title,
        string department,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax)
    {
        Guard.Required(title, nameof(Title));
        Guard.Required(department, nameof(Department));
        Guard.Required(description, nameof(Description));
        Guard.Required(location, nameof(Location));

        if (!Enum.IsDefined(employmentType))
        {
            throw new DomainValidationException("EmploymentType must be FullTime, PartTime or Temporary.");
        }

        if (salaryMin <= 0)
        {
            throw new DomainValidationException("SalaryMin must be greater than zero.");
        }

        if (salaryMax < salaryMin)
        {
            throw new DomainValidationException("SalaryMax must be greater than or equal to SalaryMin.");
        }
    }

    private static void EnsureClosingDateIsNotInThePast(DateOnly closingDate, DateTimeOffset now)
    {
        if (closingDate < ToDate(now))
        {
            throw new DomainValidationException("ClosingDate can't be in the past.");
        }
    }

    private static void EnsureClosingDateIsAfterToday(DateOnly closingDate, DateTimeOffset now)
    {
        if (closingDate <= ToDate(now))
        {
            throw new InvalidStatusTransitionException("An Open job posting must have a closing date after today.");
        }
    }

    private static DateOnly ToDate(DateTimeOffset value) => DateOnly.FromDateTime(value.UtcDateTime);
}

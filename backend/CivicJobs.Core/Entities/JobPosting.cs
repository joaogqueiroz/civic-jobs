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

        EnsureRequired(title, nameof(Title));
        EnsureRequired(department, nameof(Department));
        EnsureRequired(description, nameof(Description));
        EnsureRequired(location, nameof(Location));

        if (salaryMin <= 0)
        {
            throw new DomainValidationException("SalaryMin must be greater than zero.");
        }

        if (salaryMax < salaryMin)
        {
            throw new DomainValidationException("SalaryMax must be greater than or equal to SalaryMin.");
        }

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

    private static void EnsureRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }
    }
}

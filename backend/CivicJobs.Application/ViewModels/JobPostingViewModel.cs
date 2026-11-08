using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

public sealed record JobPostingViewModel(
    Guid Id,
    string Title,
    string Department,
    string Description,
    string Location,
    EmploymentType EmploymentType,
    decimal SalaryMin,
    decimal SalaryMax,
    DateOnly ClosingDate,
    JobPostingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static JobPostingViewModel FromEntity(JobPosting posting) => new(
        posting.Id,
        posting.Title,
        posting.Department,
        posting.Description,
        posting.Location,
        posting.EmploymentType,
        posting.SalaryMin,
        posting.SalaryMax,
        posting.ClosingDate,
        posting.Status,
        posting.CreatedAt,
        posting.UpdatedAt);
}

using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;

namespace CivicJobs.Application.ViewModels;

/// <summary>What candidates see in the public list (FR-11): no owner, status or internal timestamps.</summary>
public sealed record PublicJobPostingViewModel(
    Guid Id,
    string Title,
    string Department,
    string Description,
    string Location,
    EmploymentType EmploymentType,
    decimal SalaryMin,
    decimal SalaryMax,
    DateOnly ClosingDate)
{
    public static PublicJobPostingViewModel FromEntity(JobPosting posting) => new(
        posting.Id,
        posting.Title,
        posting.Department,
        posting.Description,
        posting.Location,
        posting.EmploymentType,
        posting.SalaryMin,
        posting.SalaryMax,
        posting.ClosingDate);
}

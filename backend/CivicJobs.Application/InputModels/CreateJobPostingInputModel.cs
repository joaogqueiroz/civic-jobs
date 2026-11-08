using CivicJobs.Core.Enums;

namespace CivicJobs.Application.InputModels;

public sealed class CreateJobPostingInputModel
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public DateOnly ClosingDate { get; set; }
}

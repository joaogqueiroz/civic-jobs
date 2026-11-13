namespace CivicJobs.Application.InputModels;

public sealed class ApplyInputModel
{
    public Guid JobPostingId { get; set; }
    public string? CoverLetter { get; set; }
}

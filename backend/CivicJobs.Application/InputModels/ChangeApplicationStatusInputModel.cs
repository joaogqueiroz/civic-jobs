using CivicJobs.Core.Enums;

namespace CivicJobs.Application.InputModels;

public sealed class ChangeApplicationStatusInputModel
{
    public JobApplicationStatus Status { get; set; }
}

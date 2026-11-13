namespace CivicJobs.Application.Services.Interfaces;

public interface IJobPostingExpirationService
{
    /// <summary>Closes every Open posting whose closing date is before today (BR-11) and returns how many were closed.</summary>
    Task<int> CloseExpiredAsync(CancellationToken cancellationToken);
}

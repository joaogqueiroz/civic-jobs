using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Core.Repositories;

namespace CivicJobs.Application.Services.Implementations;

public sealed class JobPostingExpirationService(
    IJobPostingRepository repository,
    TimeProvider timeProvider) : IJobPostingExpirationService
{
    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expired = await repository.ListExpiredOpenAsync(DateOnly.FromDateTime(now.UtcDateTime), cancellationToken);

        foreach (var posting in expired)
        {
            posting.Close(now);
            await repository.UpdateAsync(posting, cancellationToken);
        }

        return expired.Count;
    }
}

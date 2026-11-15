using CivicJobs.Application.Services.Interfaces;

namespace CivicJobs.Jobs.Api.BackgroundServices;

/// <summary>
/// Closes expired Open postings (BR-11) at startup and then every hour. It only runs in the Jobs API,
/// so the two APIs sharing the database don't both do the same work.
/// </summary>
public sealed class JobPostingExpirationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<JobPostingExpirationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        do
        {
            await CloseExpiredAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CloseExpiredAsync(CancellationToken stoppingToken)
    {
        // One failed run must not stop the worker: the next tick tries again.
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var closed = await scope.ServiceProvider
                .GetRequiredService<IJobPostingExpirationService>()
                .CloseExpiredAsync(stoppingToken);

            if (closed > 0)
            {
                logger.LogInformation("Closed {Count} job postings whose closing date has passed", closed);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Closing expired job postings failed");
        }
    }
}

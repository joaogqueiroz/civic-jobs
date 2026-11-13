using CivicJobs.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CivicJobs.Infrastructure.Email;

/// <summary>Writes each email to the log instead of sending it (PRD section 10), so the demo needs no mail server.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}{NewLine}{Body}", to, subject, Environment.NewLine, body);

        return Task.CompletedTask;
    }
}

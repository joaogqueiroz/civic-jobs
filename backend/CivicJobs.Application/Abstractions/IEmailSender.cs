namespace CivicJobs.Application.Abstractions;

/// <summary>Sends notifications to candidates (FR-23). This version writes them to the log; SMTP only needs a new implementation.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

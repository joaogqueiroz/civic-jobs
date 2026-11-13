using CivicJobs.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Email;

public class LoggingEmailSenderTests
{
    [Fact]
    [Trait("Requirement", "FR-23")]
    public async Task SendAsync_WritesRecipientSubjectAndBodyToTheLogAsInformation()
    {
        var logger = new CapturingLogger();

        await new LoggingEmailSender(logger).SendAsync(
            "candidate@example.com", "We received your application for Park Ranger", "Hello Alex", CancellationToken.None);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("candidate@example.com");
        entry.Message.ShouldContain("We received your application for Park Ranger");
        entry.Message.ShouldContain("Hello Alex");
    }

    private sealed class CapturingLogger : ILogger<LoggingEmailSender>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}

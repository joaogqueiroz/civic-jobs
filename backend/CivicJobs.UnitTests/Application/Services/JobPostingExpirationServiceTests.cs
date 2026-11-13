using CivicJobs.Application.Services.Implementations;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class JobPostingExpirationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IJobPostingRepository _repository = Substitute.For<IJobPostingRepository>();

    [Fact]
    [Trait("Rule", "BR-11")]
    [Trait("Requirement", "FR-22")]
    public async Task CloseExpiredAsync_WithExpiredOpenPostings_ClosesEachOneNow()
    {
        var first = ExpiredOpenPosting();
        var second = ExpiredOpenPosting();
        _repository.ListExpiredOpenAsync(Today, Arg.Any<CancellationToken>()).Returns([first, second]);

        await CreateService().CloseExpiredAsync(CancellationToken.None);

        new[] { first, second }.ShouldAllBe(p => p.Status == JobPostingStatus.Closed && p.UpdatedAt == Now);
    }

    [Fact]
    [Trait("Rule", "BR-11")]
    public async Task CloseExpiredAsync_WithExpiredOpenPostings_SavesEachClosedPosting()
    {
        var posting = ExpiredOpenPosting();
        _repository.ListExpiredOpenAsync(Today, Arg.Any<CancellationToken>()).Returns([posting]);

        await CreateService().CloseExpiredAsync(CancellationToken.None);

        await _repository.Received(1).UpdateAsync(
            Arg.Is<JobPosting>(p => p.Id == posting.Id && p.Status == JobPostingStatus.Closed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Rule", "BR-11")]
    public async Task CloseExpiredAsync_ReturnsHowManyPostingsWereClosed()
    {
        _repository.ListExpiredOpenAsync(Today, Arg.Any<CancellationToken>())
            .Returns([ExpiredOpenPosting(), ExpiredOpenPosting()]);

        var closed = await CreateService().CloseExpiredAsync(CancellationToken.None);

        closed.ShouldBe(2);
    }

    [Fact]
    [Trait("Rule", "BR-11")]
    public async Task CloseExpiredAsync_WithNothingExpired_ReturnsZero()
    {
        _repository.ListExpiredOpenAsync(Today, Arg.Any<CancellationToken>()).Returns([]);

        var closed = await CreateService().CloseExpiredAsync(CancellationToken.None);

        closed.ShouldBe(0);
    }

    private JobPostingExpirationService CreateService() => new(_repository, _timeProvider);

    /// <summary>An Open posting whose closing date was yesterday, created and published a month ago.</summary>
    private static JobPosting ExpiredOpenPosting()
    {
        var createdAt = Now.AddDays(-30);
        var posting = JobPosting.Create(
            createdByUserId: Guid.NewGuid(),
            title: "Park Ranger",
            department: "Parks & Recreation",
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.FullTime,
            salaryMin: 50_000m,
            salaryMax: 60_000m,
            closingDate: Today.AddDays(-1),
            createdAt: createdAt);
        posting.Publish(createdAt);
        return posting;
    }
}

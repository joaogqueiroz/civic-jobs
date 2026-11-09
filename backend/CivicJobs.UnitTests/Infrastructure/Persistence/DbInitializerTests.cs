using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Auth;
using CivicJobs.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// INTEGRATION TESTS: DbInitializer against a real SQLite in-memory database, built with the real migrations.
/// Seed data comes from docs/user-story.md.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DbInitializerTests : IDisposable
{
    private const string FirstRecruiter = "recruiter@cityofexample.gov";
    private const string SecondRecruiter = "recruiter2@cityofexample.gov";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly PasswordHasher _passwordHasher = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));

    public DbInitializerTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task InitializeAsync_OnEmptyDatabase_AppliesAllMigrations()
    {
        await InitializeAsync();

        await using var context = CreateContext();
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task InitializeAsync_OnEmptyDatabase_SeedsTwoRecruitersWithDemoPassword()
    {
        await InitializeAsync();

        await using var context = CreateContext();
        var users = await context.Users.ToListAsync();
        users.Select(u => u.Email).ShouldBe([FirstRecruiter, SecondRecruiter], ignoreOrder: true);
        users.ShouldAllBe(u => _passwordHasher.Verify(u.PasswordHash, "Demo@12345"));
    }

    [Fact]
    public async Task InitializeAsync_OnEmptyDatabase_SeedsFirstRecruiterWithTwoDraftTwoOpenAndOneClosedPosting()
    {
        await InitializeAsync();

        await using var context = CreateContext();
        var owner = await context.Users.SingleAsync(u => u.Email == FirstRecruiter);
        var postings = await context.JobPostings.Where(p => p.CreatedByUserId == owner.Id).ToListAsync();

        postings.ShouldSatisfyAllConditions(
            () => postings.Count(p => p.Status == JobPostingStatus.Draft).ShouldBe(2),
            () => postings.Count(p => p.Status == JobPostingStatus.Open).ShouldBe(2),
            () => postings.Count(p => p.Status == JobPostingStatus.Closed).ShouldBe(1),
            () => postings.ShouldAllBe(p => p.Department == "Parks & Recreation"));
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task InitializeAsync_OnEmptyDatabase_SeedsSecondRecruiterWithTwoPublicWorksPostings()
    {
        await InitializeAsync();

        await using var context = CreateContext();
        var owner = await context.Users.SingleAsync(u => u.Email == SecondRecruiter);
        var postings = await context.JobPostings.Where(p => p.CreatedByUserId == owner.Id).ToListAsync();

        postings.Count.ShouldBe(2);
        postings.ShouldAllBe(p => p.Department == "Public Works");
    }

    [Fact]
    public async Task InitializeAsync_WhenRunTwice_DoesNotDuplicateSeedData()
    {
        await InitializeAsync();
        await InitializeAsync();

        await using var context = CreateContext();
        (await context.Users.CountAsync()).ShouldBe(2);
        (await context.JobPostings.CountAsync()).ShouldBe(7);
    }

    private async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await DbInitializer.InitializeAsync(context, _passwordHasher, _timeProvider, CancellationToken.None);
    }

    private CivicJobsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CivicJobsDbContext>().UseSqlite(_connection).Options);
}

using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// INTEGRATION TESTS: JobPostingRepository against a real SQLite in-memory database (no mocks).
/// The owner and status filtering for "list my postings" (FR-05, BR-08) happens in SQL,
/// so it is verified here, not in JobPostingServiceTests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class JobPostingRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly SqliteTestDatabase _database = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    #region List by owner and status

    [Fact]
    [Trait("Requirement", "FR-05")]
    [Trait("Rule", "BR-08")]
    public async Task ListByOwnerAsync_ReturnsOnlyPostingsOfThatOwner()
    {
        var mine = await SeedAsync(Posting(_ownerId));
        await SeedAsync(Posting(Guid.NewGuid()));

        var result = await Repository().ListByOwnerAsync(_ownerId, null, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([mine.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-05")]
    public async Task ListByOwnerAsync_WithStatus_ReturnsOnlyPostingsWithThatStatus()
    {
        await SeedAsync(Posting(_ownerId));
        var open = Posting(_ownerId);
        open.Publish(Now);
        await SeedAsync(open);

        var result = await Repository().ListByOwnerAsync(_ownerId, JobPostingStatus.Open, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([open.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-05")]
    public async Task ListByOwnerAsync_WithoutStatus_ReturnsNewestFirst()
    {
        var older = await SeedAsync(Posting(_ownerId, createdAt: Now));
        var newer = await SeedAsync(Posting(_ownerId, createdAt: Now.AddMinutes(5)));

        var result = await Repository().ListByOwnerAsync(_ownerId, null, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([newer.Id, older.Id]);
    }

    #endregion

    #region Get, add, update, delete

    [Fact]
    [Trait("Requirement", "FR-04")]
    public async Task AddAsync_SavesPostingWithAllFields()
    {
        var posting = Posting(_ownerId);

        await Repository().AddAsync(posting, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(posting.Id, CancellationToken.None);
        stored.ShouldNotBeNull();
        stored.ShouldSatisfyAllConditions(
            () => stored.Title.ShouldBe(posting.Title),
            () => stored.Department.ShouldBe(posting.Department),
            () => stored.Description.ShouldBe(posting.Description),
            () => stored.Location.ShouldBe(posting.Location),
            () => stored.EmploymentType.ShouldBe(posting.EmploymentType),
            () => stored.SalaryMin.ShouldBe(posting.SalaryMin),
            () => stored.SalaryMax.ShouldBe(posting.SalaryMax),
            () => stored.ClosingDate.ShouldBe(posting.ClosingDate),
            () => stored.Status.ShouldBe(JobPostingStatus.Draft),
            () => stored.CreatedByUserId.ShouldBe(_ownerId),
            () => stored.CreatedAt.ShouldBe(posting.CreatedAt),
            () => stored.UpdatedAt.ShouldBe(posting.UpdatedAt));
    }

    [Fact]
    [Trait("Requirement", "FR-06")]
    public async Task GetByIdAsync_WhenPostingDoesNotExist_ReturnsNull()
    {
        var result = await Repository().GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Requirement", "FR-08")]
    public async Task UpdateAsync_SavesChanges()
    {
        var posting = await SeedAsync(Posting(_ownerId));
        var repository = Repository();
        var loaded = await repository.GetByIdAsync(posting.Id, CancellationToken.None);
        loaded!.Publish(Now.AddHours(1));

        await repository.UpdateAsync(loaded, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(posting.Id, CancellationToken.None);
        stored!.Status.ShouldBe(JobPostingStatus.Open);
    }

    [Fact]
    [Trait("Requirement", "FR-10")]
    public async Task DeleteAsync_RemovesPosting()
    {
        var posting = await SeedAsync(Posting(_ownerId));
        var repository = Repository();
        var loaded = await repository.GetByIdAsync(posting.Id, CancellationToken.None);

        await repository.DeleteAsync(loaded!, CancellationToken.None);

        (await Repository().GetByIdAsync(posting.Id, CancellationToken.None)).ShouldBeNull();
    }

    #endregion

    #region Public list of open postings (FR-11)

    [Fact]
    [Trait("Requirement", "FR-11")]
    public async Task ListOpenAsync_ReturnsOpenPostingsFromAllRecruitersOnly()
    {
        var otherOwner = Guid.NewGuid();
        await SeedAsync(Posting(_ownerId));
        var openMine = await SeedAsync(Published(Posting(_ownerId)));
        var openOther = await SeedAsync(Published(Posting(otherOwner)));
        var closed = Published(Posting(otherOwner));
        closed.Close(Now);
        await SeedAsync(closed);

        var result = await Repository().ListOpenAsync(Today, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([openMine.Id, openOther.Id], ignoreOrder: true);
    }

    [Fact]
    [Trait("Requirement", "FR-11")]
    public async Task ListOpenAsync_ExcludesOpenPostingsWhoseClosingDateHasPassed()
    {
        var closesToday = await SeedAsync(OpenPostingClosingOn(Today));
        await SeedAsync(OpenPostingClosingOn(Today.AddDays(-1)));

        var result = await Repository().ListOpenAsync(Today, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([closesToday.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-11")]
    public async Task ListOpenAsync_OrdersBySoonestClosingDate()
    {
        var later = await SeedAsync(OpenPostingClosingOn(Today.AddDays(20)));
        var sooner = await SeedAsync(OpenPostingClosingOn(Today.AddDays(5)));

        var result = await Repository().ListOpenAsync(Today, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([sooner.Id, later.Id]);
    }

    #endregion
    private static JobPosting Published(JobPosting posting)
    {
        posting.Publish(posting.CreatedAt);
        return posting;
    }

    /// <summary>An Open posting with the given closing date, created and published early enough for the rules to allow it.</summary>
    private static JobPosting OpenPostingClosingOn(DateOnly closingDate)
    {
        var createdAt = new DateTimeOffset(closingDate.AddDays(-30).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var posting = Posting(Guid.NewGuid(), createdAt, closingDate);
        posting.Publish(createdAt);
        return posting;
    }
    private JobPostingRepository Repository() => new(_database.CreateContext());

    private async Task<JobPosting> SeedAsync(JobPosting posting)
    {
        await Repository().AddAsync(posting, CancellationToken.None);
        return posting;
    }

    private static JobPosting Posting(Guid ownerId, DateTimeOffset? createdAt = null, DateOnly? closingDate = null) =>
        JobPosting.Create(
            createdByUserId: ownerId,
            title: "Park Ranger",
            department: "Parks & Recreation",
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.PartTime,
            salaryMin: 50_000.50m,
            salaryMax: 60_000m,
            closingDate: closingDate ?? new DateOnly(2026, 12, 31),
            createdAt: createdAt ?? Now);
}

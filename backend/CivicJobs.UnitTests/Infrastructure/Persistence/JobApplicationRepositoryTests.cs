using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// INTEGRATION TESTS: JobApplicationRepository, plus the id lookups used to show applications,
/// against a real SQLite in-memory database. The one-application-per-posting rule (BR-18) is a unique index.
/// </summary>
[Trait("Category", "Integration")]
public sealed class JobApplicationRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly SqliteTestDatabase _database = new();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly JobPosting _posting = OpenPosting();

    public void Dispose() => _database.Dispose();

    [Fact]
    [Trait("Requirement", "FR-18")]
    public async Task AddAsync_SavesApplicationThatCanBeFoundById()
    {
        var application = JobApplication.Submit(_posting, _candidateId, "I love parks.", Now);

        await Repository().AddAsync(application, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(application.Id, CancellationToken.None);
        stored.ShouldNotBeNull();
        stored.ShouldSatisfyAllConditions(
            () => stored.JobPostingId.ShouldBe(_posting.Id),
            () => stored.CandidateId.ShouldBe(_candidateId),
            () => stored.CoverLetter.ShouldBe("I love parks."),
            () => stored.Status.ShouldBe(JobApplicationStatus.Submitted),
            () => stored.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task ExistsAsync_IsTrueOnlyForTheSameCandidateAndPosting()
    {
        await SeedAsync(JobApplication.Submit(_posting, _candidateId, null, Now));

        (await Repository().ExistsAsync(_posting.Id, _candidateId, CancellationToken.None)).ShouldBeTrue();
        (await Repository().ExistsAsync(_posting.Id, Guid.NewGuid(), CancellationToken.None)).ShouldBeFalse();
        (await Repository().ExistsAsync(Guid.NewGuid(), _candidateId, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task AddAsync_SecondApplicationToTheSamePosting_ThrowsConflictException()
    {
        // Two clicks on Apply at the same time both pass the service check; the unique index catches the second.
        await SeedAsync(JobApplication.Submit(_posting, _candidateId, null, Now));

        var addDuplicate = () => Repository().AddAsync(JobApplication.Submit(_posting, _candidateId, null, Now), CancellationToken.None);

        await addDuplicate.ShouldThrowAsync<ConflictException>();
    }

    [Fact]
    [Trait("Requirement", "FR-19")]
    public async Task ListByCandidateAsync_ReturnsOnlyThatCandidatesApplicationsNewestFirst()
    {
        var older = await SeedAsync(JobApplication.Submit(_posting, _candidateId, null, Now));
        var newer = await SeedAsync(JobApplication.Submit(OpenPosting(), _candidateId, null, Now.AddMinutes(5)));
        await SeedAsync(JobApplication.Submit(_posting, Guid.NewGuid(), null, Now));

        var result = await Repository().ListByCandidateAsync(_candidateId, CancellationToken.None);

        result.Select(a => a.Id).ShouldBe([newer.Id, older.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-20")]
    public async Task ListByPostingAsync_ReturnsOnlyThatPostingsApplicationsOldestFirst()
    {
        var first = await SeedAsync(JobApplication.Submit(_posting, Guid.NewGuid(), null, Now));
        var second = await SeedAsync(JobApplication.Submit(_posting, Guid.NewGuid(), null, Now.AddMinutes(5)));
        await SeedAsync(JobApplication.Submit(OpenPosting(), _candidateId, null, Now));

        var result = await Repository().ListByPostingAsync(_posting.Id, CancellationToken.None);

        result.Select(a => a.Id).ShouldBe([first.Id, second.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-21")]
    public async Task UpdateAsync_SavesTheNewStatus()
    {
        var application = await SeedAsync(JobApplication.Submit(_posting, _candidateId, null, Now));
        var repository = Repository();
        var loaded = await repository.GetByIdAsync(application.Id, CancellationToken.None);
        loaded!.ChangeStatus(JobApplicationStatus.UnderReview, Now.AddHours(1));

        await repository.UpdateAsync(loaded, CancellationToken.None);

        (await Repository().GetByIdAsync(application.Id, CancellationToken.None))!.Status.ShouldBe(JobApplicationStatus.UnderReview);
    }

    [Fact]
    [Trait("Requirement", "FR-19")]
    public async Task PostingListByIdsAsync_ReturnsOnlyTheRequestedPostings()
    {
        var postings = new JobPostingRepository(_database.CreateContext());
        var other = OpenPosting();
        await postings.AddAsync(_posting, CancellationToken.None);
        await new JobPostingRepository(_database.CreateContext()).AddAsync(other, CancellationToken.None);

        var result = await new JobPostingRepository(_database.CreateContext()).ListByIdsAsync([_posting.Id], CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([_posting.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-20")]
    public async Task UserListByIdsAsync_ReturnsOnlyTheRequestedUsers()
    {
        var alex = User.CreateCandidate("alex@example.com", "Alex Kim", "hashed-password", Now);
        await new UserRepository(_database.CreateContext()).AddAsync(alex, CancellationToken.None);
        await new UserRepository(_database.CreateContext()).AddAsync(
            User.CreateCandidate("pat@example.com", "Pat Lee", "hashed-password", Now), CancellationToken.None);

        var result = await new UserRepository(_database.CreateContext()).ListByIdsAsync([alex.Id], CancellationToken.None);

        result.Select(u => u.Id).ShouldBe([alex.Id]);
    }

    private JobApplicationRepository Repository() => new(_database.CreateContext());

    private async Task<JobApplication> SeedAsync(JobApplication application)
    {
        await Repository().AddAsync(application, CancellationToken.None);
        return application;
    }

    private static JobPosting OpenPosting()
    {
        var createdAt = Now.AddDays(-5);
        var posting = JobPosting.Create(Guid.NewGuid(), Guid.NewGuid(), "Parks & Recreation", "Park Ranger",
            "Patrol city parks.", "Central Park District", EmploymentType.FullTime, 50_000m, 60_000m, Today.AddDays(30), createdAt);
        posting.SubmitForApproval(createdAt);
        posting.Approve(createdAt);
        return posting;
    }
}

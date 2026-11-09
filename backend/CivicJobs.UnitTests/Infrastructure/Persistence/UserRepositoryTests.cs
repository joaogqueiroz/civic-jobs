using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// INTEGRATION TESTS: UserRepository against a real SQLite in-memory database (no mocks).
/// The unique email index is only enforced by the database, so the duplicate-email case is verified here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    [Trait("Requirement", "FR-01")]
    public async Task AddAsync_SavesUserThatCanBeFoundByEmailAndId()
    {
        var user = NewUser();

        await Repository().AddAsync(user, CancellationToken.None);

        var byEmail = await Repository().GetByEmailAsync(user.Email, CancellationToken.None);
        var byId = await Repository().GetByIdAsync(user.Id, CancellationToken.None);
        byEmail.ShouldNotBeNull();
        byEmail.ShouldSatisfyAllConditions(
            () => byEmail.Id.ShouldBe(user.Id),
            () => byEmail.FullName.ShouldBe(user.FullName),
            () => byEmail.PasswordHash.ShouldBe(user.PasswordHash),
            () => byEmail.CreatedAt.ShouldBe(Now));
        byId.ShouldNotBeNull();
        byId.Email.ShouldBe(user.Email);
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task GetByEmailAsync_WhenEmailIsUnknown_ReturnsNull()
    {
        var result = await Repository().GetByEmailAsync("nobody@cityofexample.gov", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task AddAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        // Two registrations arriving at the same time can both pass the service's "email in use" check.
        // The unique index catches the second one, and it must surface as 409, not as a 500.
        await Repository().AddAsync(NewUser(), CancellationToken.None);

        var addDuplicate = () => Repository().AddAsync(NewUser(), CancellationToken.None);

        await addDuplicate.ShouldThrowAsync<ConflictException>();
    }

    private UserRepository Repository() => new(_database.CreateContext());

    private static User NewUser() => User.Create("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);
}

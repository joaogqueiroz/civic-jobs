using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
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

    [Fact]
    [Trait("Rule", "BR-13")]
    public async Task AddAsync_SavesRoleAgencyAndDepartment()
    {
        var agencyId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var staff = Staff("recruiter@cityofexample.gov", UserRole.Recruiter, agencyId, departmentId);

        await Repository().AddAsync(staff, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(staff.Id, CancellationToken.None);
        stored.ShouldNotBeNull();
        stored.ShouldSatisfyAllConditions(
            () => stored.Role.ShouldBe(UserRole.Recruiter),
            () => stored.AgencyId.ShouldBe(agencyId),
            () => stored.DepartmentId.ShouldBe(departmentId));
    }

    [Fact]
    [Trait("Requirement", "FR-13")]
    [Trait("Rule", "BR-15")]
    public async Task ListStaffByAgencyAsync_ReturnsOnlyStaffOfThatAgencyByName()
    {
        var agencyId = Guid.NewGuid();
        var zoe = await SeedAsync(Staff("zoe@cityofexample.gov", UserRole.Admin, agencyId, null, "Zoe Admin"));
        var ana = await SeedAsync(Staff("ana@cityofexample.gov", UserRole.Recruiter, agencyId, Guid.NewGuid(), "Ana Recruiter"));
        await SeedAsync(Staff("other@county.gov", UserRole.Admin, Guid.NewGuid(), null));
        await SeedAsync(User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now));

        var result = await Repository().ListStaffByAgencyAsync(agencyId, CancellationToken.None);

        result.Select(u => u.Id).ShouldBe([ana.Id, zoe.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-14")]
    public async Task UpdateAsync_SavesNewRoleAndDepartment()
    {
        var staff = await SeedAsync(Staff("recruiter@cityofexample.gov", UserRole.Recruiter, Guid.NewGuid(), Guid.NewGuid()));
        var newDepartment = Guid.NewGuid();
        var repository = Repository();
        var loaded = await repository.GetByIdAsync(staff.Id, CancellationToken.None);
        loaded!.ChangeRole(UserRole.HiringManager, newDepartment);

        await repository.UpdateAsync(loaded, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(staff.Id, CancellationToken.None);
        stored!.Role.ShouldBe(UserRole.HiringManager);
        stored.DepartmentId.ShouldBe(newDepartment);
    }

    private async Task<User> SeedAsync(User user)
    {
        await Repository().AddAsync(user, CancellationToken.None);
        return user;
    }

    private static User Staff(string email, UserRole role, Guid agencyId, Guid? departmentId, string fullName = "Sam Patel") =>
        User.CreateStaff(email, fullName, "hashed-password", role, agencyId, departmentId, Now);

    private UserRepository Repository() => new(_database.CreateContext());

    private static User NewUser() => User.CreateCandidate("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);
}

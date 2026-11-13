using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Entities;
using CivicJobs.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// INTEGRATION TESTS: DepartmentRepository and AgencyRepository against a real SQLite in-memory database.
/// The case-insensitive unique name per agency (BR-14) is enforced by the database, so it is verified here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DepartmentRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();
    private readonly Guid _agencyId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    [Trait("Requirement", "FR-12")]
    public async Task AddAsync_SavesDepartmentThatCanBeFoundById()
    {
        var department = Department.Create(_agencyId, "Libraries", Now);

        await Repository().AddAsync(department, CancellationToken.None);

        var stored = await Repository().GetByIdAsync(department.Id, CancellationToken.None);
        stored.ShouldNotBeNull();
        stored.ShouldSatisfyAllConditions(
            () => stored.AgencyId.ShouldBe(_agencyId),
            () => stored.Name.ShouldBe("Libraries"),
            () => stored.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Requirement", "FR-12")]
    [Trait("Rule", "BR-15")]
    public async Task ListByAgencyAsync_ReturnsOnlyThatAgencysDepartmentsByName()
    {
        var works = await SeedAsync(Department.Create(_agencyId, "Public Works", Now));
        var libraries = await SeedAsync(Department.Create(_agencyId, "Libraries", Now));
        await SeedAsync(Department.Create(Guid.NewGuid(), "Animal Services", Now));

        var result = await Repository().ListByAgencyAsync(_agencyId, CancellationToken.None);

        result.Select(d => d.Id).ShouldBe([libraries.Id, works.Id]);
    }

    [Theory]
    [Trait("Rule", "BR-14")]
    [InlineData("Libraries", true)]
    [InlineData("LIBRARIES", true)]
    [InlineData("Public Works", false)]
    public async Task ExistsAsync_ComparesNamesWithinTheAgencyIgnoringCase(string name, bool expected)
    {
        await SeedAsync(Department.Create(_agencyId, "Libraries", Now));
        await SeedAsync(Department.Create(Guid.NewGuid(), "Public Works", Now));

        var exists = await Repository().ExistsAsync(_agencyId, name, CancellationToken.None);

        exists.ShouldBe(expected);
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public async Task AddAsync_WhenNameExistsInTheAgencyWithOtherCase_ThrowsConflictException()
    {
        // Two admins creating the same department at once both pass the service check; the unique index catches it.
        await SeedAsync(Department.Create(_agencyId, "Libraries", Now));

        var addDuplicate = () => Repository().AddAsync(Department.Create(_agencyId, "libraries", Now), CancellationToken.None);

        await addDuplicate.ShouldThrowAsync<ConflictException>();
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public async Task AddAsync_WithSameNameInAnotherAgency_Saves()
    {
        var otherAgency = Guid.NewGuid();
        await SeedAsync(Department.Create(_agencyId, "Libraries", Now));

        await SeedAsync(Department.Create(otherAgency, "Libraries", Now));

        (await Repository().ExistsAsync(otherAgency, "Libraries", CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task AgencyGetByIdAsync_ReturnsSavedAgency()
    {
        var agency = Agency.Create("City of Example", Now);
        await using (var context = _database.CreateContext())
        {
            context.Agencies.Add(agency);
            await context.SaveChangesAsync();
        }

        var stored = await new AgencyRepository(_database.CreateContext()).GetByIdAsync(agency.Id, CancellationToken.None);

        stored.ShouldNotBeNull();
        stored.Name.ShouldBe("City of Example");
    }

    private DepartmentRepository Repository() => new(_database.CreateContext());

    private async Task<Department> SeedAsync(Department department)
    {
        await Repository().AddAsync(department, CancellationToken.None);
        return department;
    }
}

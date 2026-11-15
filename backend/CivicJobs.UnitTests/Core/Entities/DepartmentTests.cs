using CivicJobs.Core.Entities;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class DepartmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Requirement", "FR-12")]
    public void Create_WithAgencyAndName_SetsFields()
    {
        var agencyId = Guid.NewGuid();

        var department = Department.Create(agencyId, "  Public Works  ", Now);

        department.ShouldSatisfyAllConditions(
            () => department.Id.ShouldNotBe(Guid.Empty),
            () => department.AgencyId.ShouldBe(agencyId),
            () => department.Name.ShouldBe("Public Works"),
            () => department.CreatedAt.ShouldBe(Now));
    }

    [Theory]
    [Trait("Rule", "BR-14")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_ThrowsDomainValidationException(string? name)
    {
        var create = () => Department.Create(Guid.NewGuid(), name!, Now);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public void Create_WithoutAgency_ThrowsDomainValidationException()
    {
        var create = () => Department.Create(Guid.Empty, "Public Works", Now);

        create.ShouldThrow<DomainValidationException>();
    }
}

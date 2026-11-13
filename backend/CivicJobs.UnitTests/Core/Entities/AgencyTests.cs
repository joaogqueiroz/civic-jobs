using CivicJobs.Core.Entities;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class AgencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Create_WithName_SetsTrimmedNameAndCreatedAt()
    {
        var agency = Agency.Create("  City of Example  ", Now);

        agency.ShouldSatisfyAllConditions(
            () => agency.Id.ShouldNotBe(Guid.Empty),
            () => agency.Name.ShouldBe("City of Example"),
            () => agency.CreatedAt.ShouldBe(Now));
    }

    [Theory]
    [Trait("Rule", "BR-13")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithoutName_ThrowsDomainValidationException(string? name)
    {
        var create = () => Agency.Create(name!, Now);

        create.ShouldThrow<DomainValidationException>();
    }
}

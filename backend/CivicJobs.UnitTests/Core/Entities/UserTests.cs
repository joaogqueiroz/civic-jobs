using CivicJobs.Core.Entities;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void Create_WithValidData_SetsFields()
    {
        var user = User.Create("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);

        user.ShouldSatisfyAllConditions(
            () => user.Id.ShouldNotBe(Guid.Empty),
            () => user.Email.ShouldBe("recruiter@cityofexample.gov"),
            () => user.FullName.ShouldBe("Jane Doe"),
            () => user.PasswordHash.ShouldBe("hashed-password"),
            () => user.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void Create_WithMixedCaseEmail_StoresEmailInLowercase()
    {
        var user = User.Create("Recruiter@CityOfExample.GOV", "Jane Doe", "hashed-password", Now);

        user.Email.ShouldBe("recruiter@cityofexample.gov");
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void Create_WithSurroundingSpacesInEmail_StoresTrimmedEmail()
    {
        var user = User.Create("  recruiter@cityofexample.gov  ", "Jane Doe", "hashed-password", Now);

        user.Email.ShouldBe("recruiter@cityofexample.gov");
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void NormalizeEmail_WithMixedCaseAndSpaces_ReturnsTrimmedLowercase()
    {
        User.NormalizeEmail(" Recruiter@CityOfExample.GOV ").ShouldBe("recruiter@cityofexample.gov");
    }

    [Theory]
    [Trait("Requirement", "FR-01")]
    [InlineData("email")]
    [InlineData("fullName")]
    [InlineData("passwordHash")]
    public void Create_WhenRequiredFieldIsMissing_ThrowsDomainValidationException(string field)
    {
        var create = () => User.Create(
            email: field == "email" ? " " : "recruiter@cityofexample.gov",
            fullName: field == "fullName" ? " " : "Jane Doe",
            passwordHash: field == "passwordHash" ? " " : "hashed-password",
            createdAt: Now);

        create.ShouldThrow<DomainValidationException>();
    }
}

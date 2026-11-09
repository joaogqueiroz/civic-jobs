using CivicJobs.Application.Exceptions;
using CivicJobs.Application.Services;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetCurrentAsync_WhenUserExists_ReturnsUser()
    {
        var user = User.Create("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await new UserService(_users).GetCurrentAsync(user.Id, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(user.Id),
            () => result.Email.ShouldBe(user.Email),
            () => result.FullName.ShouldBe(user.FullName),
            () => result.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetCurrentAsync_WhenUserDoesNotExist_ThrowsUnauthorizedException()
    {
        var get = () => new UserService(_users).GetCurrentAsync(Guid.NewGuid(), CancellationToken.None);

        (await get.ShouldThrowAsync<UnauthorizedException>())
            .Message.ShouldBe("Your session has ended. Log in again to continue.");
    }
}

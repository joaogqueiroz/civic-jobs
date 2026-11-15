using CivicJobs.Application.Exceptions;
using CivicJobs.Application.Services.Implementations;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAgencyRepository _agencies = Substitute.For<IAgencyRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetCurrentAsync_WhenUserExists_ReturnsUser()
    {
        var user = User.CreateCandidate("candidate@example.com", "Jane Doe", "hashed-password", Now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateService().GetCurrentAsync(user.Id, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(user.Id),
            () => result.Email.ShouldBe(user.Email),
            () => result.FullName.ShouldBe(user.FullName),
            () => result.Role.ShouldBe(UserRole.Candidate),
            () => result.AgencyName.ShouldBeNull(),
            () => result.DepartmentName.ShouldBeNull(),
            () => result.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetCurrentAsync_ForStaffUser_ReturnsRoleAgencyAndDepartmentNames()
    {
        var agency = Agency.Create("City of Example", Now);
        var department = Department.Create(agency.Id, "Public Works", Now);
        var user = User.CreateStaff("staff@cityofexample.gov", "Sam Patel", "hashed-password",
            UserRole.HiringManager, agency.Id, department.Id, Now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _agencies.GetByIdAsync(agency.Id, Arg.Any<CancellationToken>()).Returns(agency);
        _departments.GetByIdAsync(department.Id, Arg.Any<CancellationToken>()).Returns(department);

        var result = await CreateService().GetCurrentAsync(user.Id, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Role.ShouldBe(UserRole.HiringManager),
            () => result.AgencyId.ShouldBe(agency.Id),
            () => result.AgencyName.ShouldBe("City of Example"),
            () => result.DepartmentId.ShouldBe(department.Id),
            () => result.DepartmentName.ShouldBe("Public Works"));
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetCurrentAsync_WhenUserDoesNotExist_ThrowsUnauthorizedException()
    {
        var get = () => CreateService().GetCurrentAsync(Guid.NewGuid(), CancellationToken.None);

        (await get.ShouldThrowAsync<UnauthorizedException>())
            .Message.ShouldBe("Your session has ended. Log in again to continue.");
    }

    private UserService CreateService() => new(_users, _agencies, _departments);
}

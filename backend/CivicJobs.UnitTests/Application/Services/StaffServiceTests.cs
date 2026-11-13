using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Implementations;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Repositories;
using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class StaffServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly Guid _agencyId = Guid.NewGuid();
    private readonly User _admin;
    private readonly Department _parks;
    private readonly Department _otherAgencyDepartment = Department.Create(Guid.NewGuid(), "Parks & Recreation", Now);

    public StaffServiceTests()
    {
        _admin = User.CreateStaff("admin@cityofexample.gov", "Casey Rivera", "hashed-password",
            UserRole.Admin, _agencyId, null, Now);
        _parks = Department.Create(_agencyId, "Parks & Recreation", Now);

        _users.GetByIdAsync(_admin.Id, Arg.Any<CancellationToken>()).Returns(_admin);
        _departments.GetByIdAsync(_parks.Id, Arg.Any<CancellationToken>()).Returns(_parks);
        _departments.GetByIdAsync(_otherAgencyDepartment.Id, Arg.Any<CancellationToken>()).Returns(_otherAgencyDepartment);
        _departments.ListByAgencyAsync(_agencyId, Arg.Any<CancellationToken>()).Returns([_parks]);
        _passwordHasher.Hash("Demo@12345").Returns("hashed-password");
    }

    #region List

    [Fact]
    [Trait("Requirement", "FR-13")]
    [Trait("Rule", "BR-15")]
    public async Task ListAsync_ReturnsStaffOfTheAgencyWithDepartmentNames()
    {
        var recruiter = StaffMember(UserRole.Recruiter, _parks.Id);
        _users.ListStaffByAgencyAsync(_agencyId, Arg.Any<CancellationToken>()).Returns([_admin, recruiter]);

        var result = await CreateService().ListAsync(_admin.Id, CancellationToken.None);

        result.Select(s => (s.Email, s.Role, s.DepartmentName)).ShouldBe(
        [
            ("admin@cityofexample.gov", UserRole.Admin, (string?)null),
            (recruiter.Email, UserRole.Recruiter, "Parks & Recreation")
        ]);
    }

    #endregion

    #region Create

    [Fact]
    [Trait("Requirement", "FR-13")]
    public async Task CreateAsync_WithRecruiterInAgencyDepartment_SavesStaffUserWithHashedPassword()
    {
        User? saved = null;
        _users
            .When(r => r.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<User>());

        var result = await CreateService().CreateAsync(_admin.Id, CreateInput(), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.Role.ShouldBe(UserRole.Recruiter),
            () => saved.AgencyId.ShouldBe(_agencyId),
            () => saved.DepartmentId.ShouldBe(_parks.Id),
            () => saved.PasswordHash.ShouldBe("hashed-password"),
            () => result.Id.ShouldBe(saved.Id),
            () => result.DepartmentName.ShouldBe("Parks & Recreation"));
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task CreateAsync_WithDepartmentOfAnotherAgency_ThrowsNotFoundException()
    {
        var input = CreateInput(departmentId: _otherAgencyDepartment.Id);

        var create = () => CreateService().CreateAsync(_admin.Id, input, CancellationToken.None);

        (await create.ShouldThrowAsync<NotFoundException>())
            .Message.ShouldBe("We couldn't find that department.");
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task CreateAsync_WhenEmailIsInUse_ThrowsConflictException()
    {
        _users.GetByEmailAsync("staff@cityofexample.gov", Arg.Any<CancellationToken>())
            .Returns(StaffMember(UserRole.Recruiter, _parks.Id));

        var create = () => CreateService().CreateAsync(_admin.Id, CreateInput(email: "Staff@CityOfExample.gov"), CancellationToken.None);

        await create.ShouldThrowAsync<ConflictException>();
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public async Task CreateAsync_WithCandidateRole_ThrowsValidationException()
    {
        var create = () => CreateService().CreateAsync(_admin.Id, CreateInput(role: UserRole.Candidate, departmentId: null), CancellationToken.None);

        (await create.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateStaffInputModel.Role));
    }

    #endregion

    #region Change role

    [Fact]
    [Trait("Requirement", "FR-14")]
    public async Task ChangeRoleAsync_ForStaffOfTheAgency_SavesNewRoleAndDepartment()
    {
        var recruiter = Stored(StaffMember(UserRole.Recruiter, _parks.Id));

        var result = await CreateService().ChangeRoleAsync(
            _admin.Id, recruiter.Id, RoleInput(UserRole.HiringManager, _parks.Id), CancellationToken.None);

        result.Role.ShouldBe(UserRole.HiringManager);
        await _users.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.Id == recruiter.Id && u.Role == UserRole.HiringManager), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Rule", "BR-22")]
    public async Task ChangeRoleAsync_OfTheAdminThemself_ThrowsConflictException()
    {
        var change = () => CreateService().ChangeRoleAsync(
            _admin.Id, _admin.Id, RoleInput(UserRole.Recruiter, _parks.Id), CancellationToken.None);

        (await change.ShouldThrowAsync<ConflictException>())
            .Message.ShouldBe("You can't change your own role. Ask another admin of your agency.");
        _admin.Role.ShouldBe(UserRole.Admin);
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task ChangeRoleAsync_ForStaffOfAnotherAgency_ThrowsNotFoundException()
    {
        var outsider = Stored(User.CreateStaff("outsider@county.gov", "Pat Lee", "hashed-password",
            UserRole.Recruiter, _otherAgencyDepartment.AgencyId, _otherAgencyDepartment.Id, Now));

        var change = () => CreateService().ChangeRoleAsync(
            _admin.Id, outsider.Id, RoleInput(UserRole.HiringManager, _parks.Id), CancellationToken.None);

        (await change.ShouldThrowAsync<NotFoundException>())
            .Message.ShouldBe("We couldn't find that staff member.");
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task ChangeRoleAsync_ForCandidate_ThrowsNotFoundException()
    {
        var candidate = Stored(User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now));

        var change = () => CreateService().ChangeRoleAsync(
            _admin.Id, candidate.Id, RoleInput(UserRole.Recruiter, _parks.Id), CancellationToken.None);

        await change.ShouldThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task ChangeRoleAsync_ToDepartmentOfAnotherAgency_ThrowsNotFoundException()
    {
        var recruiter = Stored(StaffMember(UserRole.Recruiter, _parks.Id));

        var change = () => CreateService().ChangeRoleAsync(
            _admin.Id, recruiter.Id, RoleInput(UserRole.HiringManager, _otherAgencyDepartment.Id), CancellationToken.None);

        await change.ShouldThrowAsync<NotFoundException>();
        recruiter.DepartmentId.ShouldBe(_parks.Id);
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public async Task ChangeRoleAsync_ToAdminWithDepartment_ThrowsValidationException()
    {
        var recruiter = Stored(StaffMember(UserRole.Recruiter, _parks.Id));

        var change = () => CreateService().ChangeRoleAsync(
            _admin.Id, recruiter.Id, RoleInput(UserRole.Admin, _parks.Id), CancellationToken.None);

        (await change.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(ChangeRoleInputModel.DepartmentId));
    }

    #endregion

    private StaffService CreateService() =>
        new(
            new CreateStaffInputModelValidator(),
            new ChangeRoleInputModelValidator(),
            _users,
            _departments,
            _passwordHasher,
            _timeProvider);

    private User StaffMember(UserRole role, Guid? departmentId) =>
        User.CreateStaff($"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@cityofexample.gov", "Sam Patel",
            "hashed-password", role, _agencyId, departmentId, Now);

    private User Stored(User user)
    {
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private CreateStaffInputModel CreateInput(
        string email = "staff@cityofexample.gov",
        UserRole role = UserRole.Recruiter,
        Guid? departmentId = null) => new()
    {
        FullName = "Sam Patel",
        Email = email,
        Password = "Demo@12345",
        Role = role,
        DepartmentId = departmentId ?? (role is UserRole.Recruiter or UserRole.HiringManager ? _parks.Id : null)
    };

    private static ChangeRoleInputModel RoleInput(UserRole role, Guid? departmentId) => new()
    {
        Role = role,
        DepartmentId = departmentId
    };
}

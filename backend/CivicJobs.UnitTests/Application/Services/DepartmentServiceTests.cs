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

public class DepartmentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly Guid _agencyId = Guid.NewGuid();
    private readonly User _admin;

    public DepartmentServiceTests()
    {
        _admin = User.CreateStaff("admin@cityofexample.gov", "Casey Rivera", "hashed-password",
            UserRole.Admin, _agencyId, null, Now);
        _users.GetByIdAsync(_admin.Id, Arg.Any<CancellationToken>()).Returns(_admin);
    }

    #region List

    [Fact]
    [Trait("Requirement", "FR-12")]
    [Trait("Rule", "BR-15")]
    public async Task ListAsync_ReturnsDepartmentsOfTheAdminsAgency()
    {
        var parks = Department.Create(_agencyId, "Parks & Recreation", Now);
        _departments.ListByAgencyAsync(_agencyId, Arg.Any<CancellationToken>()).Returns([parks]);

        var result = await CreateService().ListAsync(_admin.Id, CancellationToken.None);

        result.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            d => d.Id.ShouldBe(parks.Id),
            d => d.Name.ShouldBe("Parks & Recreation"),
            d => d.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Requirement", "FR-12")]
    public async Task ListAsync_WhenAdminNoLongerExists_ThrowsUnauthorizedException()
    {
        var list = () => CreateService().ListAsync(Guid.NewGuid(), CancellationToken.None);

        await list.ShouldThrowAsync<UnauthorizedException>();
    }

    #endregion

    #region Create

    [Fact]
    [Trait("Requirement", "FR-12")]
    public async Task CreateAsync_WithNewName_SavesDepartmentInTheAdminsAgency()
    {
        Department? saved = null;
        _departments
            .When(r => r.AddAsync(Arg.Any<Department>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Department>());

        var result = await CreateService().CreateAsync(_admin.Id, Input("  Libraries "), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.AgencyId.ShouldBe(_agencyId),
            () => saved.Name.ShouldBe("Libraries"),
            () => result.Id.ShouldBe(saved.Id),
            () => result.Name.ShouldBe("Libraries"));
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public async Task CreateAsync_WhenNameIsAlreadyUsedInTheAgency_ThrowsConflictException()
    {
        _departments.ExistsAsync(_agencyId, "Libraries", Arg.Any<CancellationToken>()).Returns(true);

        var create = () => CreateService().CreateAsync(_admin.Id, Input("Libraries"), CancellationToken.None);

        (await create.ShouldThrowAsync<ConflictException>())
            .Message.ShouldBe("Your agency already has a department with this name.");
        await _departments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public async Task CreateAsync_WithInvalidName_ThrowsValidationException()
    {
        var create = () => CreateService().CreateAsync(_admin.Id, Input(""), CancellationToken.None);

        (await create.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateDepartmentInputModel.Name));
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task CreateAsync_WhenCallerHasNoAgency_ThrowsUnauthorizedException()
    {
        var candidate = User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now);
        _users.GetByIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(candidate);

        var create = () => CreateService().CreateAsync(candidate.Id, Input("Libraries"), CancellationToken.None);

        await create.ShouldThrowAsync<UnauthorizedException>();
    }

    #endregion

    private DepartmentService CreateService() =>
        new(new CreateDepartmentInputModelValidator(), _users, _departments, _timeProvider);

    private static CreateDepartmentInputModel Input(string name) => new() { Name = name };
}

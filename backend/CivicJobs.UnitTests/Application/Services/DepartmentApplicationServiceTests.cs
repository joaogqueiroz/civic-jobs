using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Implementations;
using CivicJobs.Application.Validators;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using CivicJobs.Core.Repositories;
using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class DepartmentApplicationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IJobApplicationRepository _applications = Substitute.For<IJobApplicationRepository>();
    private readonly IJobPostingRepository _postings = Substitute.For<IJobPostingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly Department _parks = Department.Create(Guid.NewGuid(), "Parks & Recreation", Now);
    private readonly User _manager;
    private readonly User _candidate = User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now);

    public DepartmentApplicationServiceTests()
    {
        _manager = User.CreateStaff("manager@cityofexample.gov", "Sam Patel", "hashed-password",
            UserRole.HiringManager, _parks.AgencyId, _parks.Id, Now);
        _users.GetByIdAsync(_manager.Id, Arg.Any<CancellationToken>()).Returns(_manager);
        _users.GetByIdAsync(_candidate.Id, Arg.Any<CancellationToken>()).Returns(_candidate);
        _departments.GetByIdAsync(_parks.Id, Arg.Any<CancellationToken>()).Returns(_parks);
    }

    #region List applications of a posting

    [Fact]
    [Trait("Requirement", "FR-20")]
    public async Task ListByPostingAsync_ReturnsApplicationsWithCandidateNameEmailAndCoverLetter()
    {
        var posting = StoredPosting(_parks.Id);
        var application = StoredApplication(posting);
        _applications.ListByPostingAsync(posting.Id, Arg.Any<CancellationToken>()).Returns([application]);
        _users.ListByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(_candidate.Id)), Arg.Any<CancellationToken>())
            .Returns([_candidate]);

        var result = await CreateService().ListByPostingAsync(_manager.Id, posting.Id, CancellationToken.None);

        result.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            a => a.Id.ShouldBe(application.Id),
            a => a.CandidateName.ShouldBe("Alex Kim"),
            a => a.CandidateEmail.ShouldBe("candidate@example.com"),
            a => a.CoverLetter.ShouldBe("I love parks."),
            a => a.Status.ShouldBe(JobApplicationStatus.Submitted));
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ListByPostingAsync_ForPostingOfAnotherDepartment_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());

        var list = () => CreateService().ListByPostingAsync(_manager.Id, posting.Id, CancellationToken.None);

        await list.ShouldThrowAsync<NotFoundException>();
    }

    #endregion

    #region Change status

    [Fact]
    [Trait("Requirement", "FR-21")]
    public async Task ChangeStatusAsync_FromSubmittedToUnderReview_SavesAndReturnsTheNewStatus()
    {
        var application = StoredApplication(StoredPosting(_parks.Id));

        var result = await CreateService().ChangeStatusAsync(
            _manager.Id, application.Id, StatusInput(JobApplicationStatus.UnderReview), CancellationToken.None);

        result.Status.ShouldBe(JobApplicationStatus.UnderReview);
        result.UpdatedAt.ShouldBe(Now);
        await _applications.Received(1).UpdateAsync(
            Arg.Is<JobApplication>(a => a.Id == application.Id && a.Status == JobApplicationStatus.UnderReview),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Rule", "BR-19")]
    public async Task ChangeStatusAsync_FromSubmittedToHired_ThrowsInvalidStatusTransitionException()
    {
        var application = StoredApplication(StoredPosting(_parks.Id));

        var change = () => CreateService().ChangeStatusAsync(
            _manager.Id, application.Id, StatusInput(JobApplicationStatus.Hired), CancellationToken.None);

        await change.ShouldThrowAsync<InvalidStatusTransitionException>();
        await _applications.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-19")]
    public async Task ChangeStatusAsync_BackToSubmitted_ThrowsValidationException()
    {
        var application = StoredApplication(StoredPosting(_parks.Id));

        var change = () => CreateService().ChangeStatusAsync(
            _manager.Id, application.Id, StatusInput(JobApplicationStatus.Submitted), CancellationToken.None);

        (await change.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(ChangeApplicationStatusInputModel.Status));
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ChangeStatusAsync_ForApplicationToAnotherDepartment_ThrowsNotFoundException()
    {
        var application = StoredApplication(StoredPosting(Guid.NewGuid()));

        var change = () => CreateService().ChangeStatusAsync(
            _manager.Id, application.Id, StatusInput(JobApplicationStatus.UnderReview), CancellationToken.None);

        (await change.ShouldThrowAsync<NotFoundException>())
            .Message.ShouldBe("We couldn't find that application in your department.");
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ChangeStatusAsync_ForUnknownApplication_ThrowsNotFoundException()
    {
        var change = () => CreateService().ChangeStatusAsync(
            _manager.Id, Guid.NewGuid(), StatusInput(JobApplicationStatus.UnderReview), CancellationToken.None);

        await change.ShouldThrowAsync<NotFoundException>();
    }

    #endregion

    private DepartmentApplicationService CreateService() =>
        new(new ChangeApplicationStatusInputModelValidator(), _applications, _postings, _users, _departments, _timeProvider);

    private static ChangeApplicationStatusInputModel StatusInput(JobApplicationStatus status) => new() { Status = status };

    private JobPosting StoredPosting(Guid departmentId)
    {
        var createdAt = Now.AddDays(-5);
        var posting = JobPosting.Create(Guid.NewGuid(), departmentId, "Parks & Recreation", "Park Ranger",
            "Patrol city parks.", "Central Park District", EmploymentType.FullTime, 50_000m, 60_000m, Today.AddDays(30), createdAt);
        posting.SubmitForApproval(createdAt);
        posting.Approve(createdAt);
        _postings.GetByIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(posting);
        return posting;
    }

    private JobApplication StoredApplication(JobPosting posting)
    {
        var application = JobApplication.Submit(posting, _candidate.Id, "I love parks.", Now.AddDays(-1));
        _applications.GetByIdAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        return application;
    }
}

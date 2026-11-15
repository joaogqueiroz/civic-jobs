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

public class DepartmentJobPostingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IJobPostingRepository _postings = Substitute.For<IJobPostingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly Department _parks = Department.Create(Guid.NewGuid(), "Parks & Recreation", Now);
    private readonly User _manager;

    public DepartmentJobPostingServiceTests()
    {
        _manager = User.CreateStaff("manager@cityofexample.gov", "Sam Patel", "hashed-password",
            UserRole.HiringManager, _parks.AgencyId, _parks.Id, Now);
        _users.GetByIdAsync(_manager.Id, Arg.Any<CancellationToken>()).Returns(_manager);
        _departments.GetByIdAsync(_parks.Id, Arg.Any<CancellationToken>()).Returns(_parks);
    }

    #region List and get

    [Fact]
    [Trait("Requirement", "FR-15")]
    [Trait("Rule", "BR-16")]
    public async Task ListAsync_ReturnsPostingsOfTheManagersDepartmentWithTheStatusFilter()
    {
        var pending = StoredPosting(JobPostingStatus.PendingApproval);
        _postings.ListByDepartmentAsync(_parks.Id, JobPostingStatus.PendingApproval, Arg.Any<CancellationToken>())
            .Returns([pending]);

        var result = await CreateService().ListAsync(_manager.Id, JobPostingStatus.PendingApproval, CancellationToken.None);

        result.ShouldHaveSingleItem().Id.ShouldBe(pending.Id);
    }

    [Fact]
    [Trait("Requirement", "FR-15")]
    public async Task GetByIdAsync_ForPostingOfTheDepartment_ReturnsIt()
    {
        var posting = StoredPosting(JobPostingStatus.Open);

        var result = await CreateService().GetByIdAsync(_manager.Id, posting.Id, CancellationToken.None);

        result.Status.ShouldBe(JobPostingStatus.Open);
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task GetByIdAsync_ForPostingOfAnotherDepartment_ThrowsNotFoundException()
    {
        var posting = StoredPosting(JobPostingStatus.Open, departmentId: Guid.NewGuid());

        var get = () => CreateService().GetByIdAsync(_manager.Id, posting.Id, CancellationToken.None);

        await get.ShouldThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task GetByIdAsync_ForDraftOfTheDepartment_ThrowsNotFoundException()
    {
        // Drafts belong to the recruiter until they are submitted.
        var posting = StoredPosting(JobPostingStatus.Draft);

        var get = () => CreateService().GetByIdAsync(_manager.Id, posting.Id, CancellationToken.None);

        await get.ShouldThrowAsync<NotFoundException>();
    }

    #endregion

    #region Approve

    [Fact]
    [Trait("Requirement", "FR-16")]
    public async Task ApproveAsync_WhenPendingApproval_SavesAndReturnsOpenPosting()
    {
        var posting = StoredPosting(JobPostingStatus.PendingApproval);

        var result = await CreateService().ApproveAsync(_manager.Id, posting.Id, CancellationToken.None);

        result.Status.ShouldBe(JobPostingStatus.Open);
        await _postings.Received(1).UpdateAsync(
            Arg.Is<JobPosting>(p => p.Id == posting.Id && p.Status == JobPostingStatus.Open), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task ApproveAsync_WhenAlreadyOpen_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(JobPostingStatus.Open);

        var approve = () => CreateService().ApproveAsync(_manager.Id, posting.Id, CancellationToken.None);

        await approve.ShouldThrowAsync<InvalidStatusTransitionException>();
        await _postings.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ApproveAsync_ForPostingOfAnotherDepartment_ThrowsNotFoundException()
    {
        var posting = StoredPosting(JobPostingStatus.PendingApproval, departmentId: Guid.NewGuid());

        var approve = () => CreateService().ApproveAsync(_manager.Id, posting.Id, CancellationToken.None);

        await approve.ShouldThrowAsync<NotFoundException>();
        posting.Status.ShouldBe(JobPostingStatus.PendingApproval);
    }

    #endregion

    #region Reject

    [Fact]
    [Trait("Requirement", "FR-17")]
    public async Task RejectAsync_WithReason_SavesDraftWithTheReason()
    {
        var posting = StoredPosting(JobPostingStatus.PendingApproval);

        var result = await CreateService().RejectAsync(
            _manager.Id, posting.Id, new RejectJobPostingInputModel { Reason = "Add the shift hours." }, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(JobPostingStatus.Draft),
            () => result.RejectionReason.ShouldBe("Add the shift hours."));
        await _postings.Received(1).UpdateAsync(Arg.Is<JobPosting>(p => p.Id == posting.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Rule", "BR-17")]
    public async Task RejectAsync_WithoutReason_ThrowsValidationException()
    {
        var posting = StoredPosting(JobPostingStatus.PendingApproval);

        var reject = () => CreateService().RejectAsync(
            _manager.Id, posting.Id, new RejectJobPostingInputModel { Reason = "" }, CancellationToken.None);

        (await reject.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(RejectJobPostingInputModel.Reason));
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task RejectAsync_ForPostingOfAnotherDepartment_ThrowsNotFoundExceptionBeforeValidating()
    {
        var posting = StoredPosting(JobPostingStatus.PendingApproval, departmentId: Guid.NewGuid());

        var reject = () => CreateService().RejectAsync(
            _manager.Id, posting.Id, new RejectJobPostingInputModel { Reason = "" }, CancellationToken.None);

        await reject.ShouldThrowAsync<NotFoundException>();
    }

    #endregion

    private DepartmentJobPostingService CreateService() =>
        new(new RejectJobPostingInputModelValidator(), _postings, _users, _departments, _timeProvider);

    private JobPosting StoredPosting(JobPostingStatus status, Guid? departmentId = null)
    {
        var posting = JobPosting.Create(
            createdByUserId: Guid.NewGuid(),
            departmentId: departmentId ?? _parks.Id,
            department: _parks.Name,
            title: "Park Ranger",
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.FullTime,
            salaryMin: 50_000m,
            salaryMax: 60_000m,
            closingDate: new DateOnly(2026, 12, 31),
            createdAt: Now);

        if (status != JobPostingStatus.Draft)
        {
            posting.SubmitForApproval(Now);
        }

        if (status == JobPostingStatus.Open)
        {
            posting.Approve(Now);
        }

        _postings.GetByIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(posting);
        return posting;
    }
}

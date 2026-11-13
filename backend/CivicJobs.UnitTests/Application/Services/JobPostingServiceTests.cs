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

public class JobPostingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IJobPostingRepository _repository = Substitute.For<IJobPostingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly Department _department = Department.Create(Guid.NewGuid(), "Parks & Recreation", Now);

    public JobPostingServiceTests()
    {
        // Any caller is a recruiter of _department unless a test says otherwise.
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Recruiter(_department.Id));
        _departments.GetByIdAsync(_department.Id, Arg.Any<CancellationToken>()).Returns(_department);
    }

    #region Create

    [Fact]
    [Trait("Requirement", "FR-04")]
    public async Task CreateAsync_WithValidInput_ReturnsDraftPostingWithInputData()
    {
        var input = ValidInput();

        var result = await CreateService().CreateAsync(Guid.NewGuid(), input, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldNotBe(Guid.Empty),
            () => result.Title.ShouldBe(input.Title),
            () => result.Department.ShouldBe(_department.Name),
            () => result.Description.ShouldBe(input.Description),
            () => result.Location.ShouldBe(input.Location),
            () => result.EmploymentType.ShouldBe(input.EmploymentType),
            () => result.SalaryMin.ShouldBe(input.SalaryMin),
            () => result.SalaryMax.ShouldBe(input.SalaryMax),
            () => result.ClosingDate.ShouldBe(input.ClosingDate),
            () => result.Status.ShouldBe(JobPostingStatus.Draft),
            () => result.CreatedAt.ShouldBe(Now),
            () => result.UpdatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public async Task CreateAsync_WithInvalidInput_ThrowsValidationException()
    {
        var input = ValidInput();
        input.Title = "ab";

        var create = () => CreateService().CreateAsync(Guid.NewGuid(), input, CancellationToken.None);

        var exception = await create.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContain(e => e.PropertyName == nameof(CreateJobPostingInputModel.Title));
        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    [Trait("Rule", "BR-08")]
    public async Task CreateAsync_WithValidInput_SavesPostingOwnedByCaller()
    {
        var userId = Guid.NewGuid();
        JobPosting? saved = null;
        _repository
            .When(r => r.AddAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobPosting>());

        var result = await CreateService().CreateAsync(userId, ValidInput(), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.Id.ShouldBe(result.Id),
            () => saved.CreatedByUserId.ShouldBe(userId));
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    [Trait("Rule", "BR-13")]
    public async Task CreateAsync_WithValidInput_SetsDepartmentFromRecruiterAccount()
    {
        JobPosting? saved = null;
        _repository
            .When(r => r.AddAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobPosting>());

        await CreateService().CreateAsync(_userId, ValidInput(), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.DepartmentId.ShouldBe(_department.Id);
        saved.Department.ShouldBe("Parks & Recreation");
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    public async Task CreateAsync_WhenUserNoLongerExists_ThrowsUnauthorizedException()
    {
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var create = () => CreateService().CreateAsync(_userId, ValidInput(), CancellationToken.None);

        await create.ShouldThrowAsync<UnauthorizedException>();
        await ShouldNotHaveSavedAnythingAsync();
    }

    #endregion

    #region Get by id

    [Fact]
    [Trait("Requirement", "FR-06")]
    public async Task GetByIdAsync_WhenOwnedByCaller_ReturnsPosting()
    {
        var posting = StoredPosting(_userId);

        var result = await CreateService().GetByIdAsync(_userId, posting.Id, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(posting.Id),
            () => result.Title.ShouldBe(posting.Title));
    }

    [Fact]
    [Trait("Requirement", "FR-06")]
    public async Task GetByIdAsync_WhenPostingDoesNotExist_ThrowsNotFoundException()
    {
        var get = () => CreateService().GetByIdAsync(_userId, Guid.NewGuid(), CancellationToken.None);

        (await get.ShouldThrowAsync<NotFoundException>())
            .Message.ShouldBe("We couldn't find that job posting. It may have been deleted.");
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task GetByIdAsync_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());

        var get = () => CreateService().GetByIdAsync(_userId, posting.Id, CancellationToken.None);

        await get.ShouldThrowAsync<NotFoundException>();
    }

    #endregion

    #region List

    // These unit tests only prove the service passes the caller's id and the status filter to the repository
    // and maps the result. The actual filtering by owner and status is done in SQL, so it is covered by the
    // integration tests in CivicJobs.UnitTests/Infrastructure/Persistence/JobPostingRepositoryTests.cs.

    [Fact]
    [Trait("Requirement", "FR-05")]
    public async Task ListAsync_WithStatus_PassesOwnerAndStatusToRepositoryAndMapsResult()
    {
        var open = StoredPosting(_userId);
        open.Publish(Now);
        _repository
            .ListByOwnerAsync(_userId, JobPostingStatus.Open, Arg.Any<CancellationToken>())
            .Returns([open]);

        var result = await CreateService().ListAsync(_userId, JobPostingStatus.Open, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([open.Id]);
    }

    [Fact]
    [Trait("Requirement", "FR-05")]
    public async Task ListAsync_WithoutStatus_PassesOwnerAndNoStatusToRepositoryAndMapsResult()
    {
        var first = StoredPosting(_userId);
        var second = StoredPosting(_userId);
        _repository
            .ListByOwnerAsync(_userId, null, Arg.Any<CancellationToken>())
            .Returns([first, second]);

        var result = await CreateService().ListAsync(_userId, null, CancellationToken.None);

        result.Select(p => p.Id).ShouldBe([first.Id, second.Id]);
    }

    #endregion

    #region Update

    [Fact]
    [Trait("Requirement", "FR-07")]
    public async Task UpdateAsync_WhenOwnedByCaller_ReturnsAndSavesUpdatedPosting()
    {
        var posting = StoredPosting(_userId);
        var input = ValidUpdateInput();
        JobPosting? saved = null;
        _repository
            .When(r => r.UpdateAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobPosting>());

        var result = await CreateService().UpdateAsync(_userId, posting.Id, input, CancellationToken.None);

        result.Title.ShouldBe(input.Title);
        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.Id.ShouldBe(posting.Id),
            () => saved.Title.ShouldBe(input.Title));
    }

    [Fact]
    [Trait("Rule", "BR-03")]
    public async Task UpdateAsync_WithInvalidInput_ThrowsValidationException()
    {
        var posting = StoredPosting(_userId);
        var input = ValidUpdateInput();
        input.Title = "";

        var update = () => CreateService().UpdateAsync(_userId, posting.Id, input, CancellationToken.None);

        await update.ShouldThrowAsync<ValidationException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task UpdateAsync_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());

        var update = () => CreateService().UpdateAsync(_userId, posting.Id, ValidUpdateInput(), CancellationToken.None);

        await update.ShouldThrowAsync<NotFoundException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task UpdateAsync_WhenOwnedByAnotherUserAndInputIsInvalid_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());
        var input = ValidUpdateInput();
        input.Title = "";

        var update = () => CreateService().UpdateAsync(_userId, posting.Id, input, CancellationToken.None);

        await update.ShouldThrowAsync<NotFoundException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task UpdateAsync_WhenClosedAndInputIsInvalid_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(_userId);
        posting.Publish(Now);
        posting.Close(Now);
        var input = ValidUpdateInput();
        input.Title = "";

        var update = () => CreateService().UpdateAsync(_userId, posting.Id, input, CancellationToken.None);

        await update.ShouldThrowAsync<InvalidStatusTransitionException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task UpdateAsync_WhenClosed_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(_userId);
        posting.Publish(Now);
        posting.Close(Now);

        var update = () => CreateService().UpdateAsync(_userId, posting.Id, ValidUpdateInput(), CancellationToken.None);

        await update.ShouldThrowAsync<InvalidStatusTransitionException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    #endregion

    #region Delete

    [Fact]
    [Trait("Requirement", "FR-10")]
    public async Task DeleteAsync_WhenDraftOwnedByCaller_DeletesPosting()
    {
        var posting = StoredPosting(_userId);
        JobPosting? deleted = null;
        _repository
            .When(r => r.DeleteAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => deleted = call.Arg<JobPosting>());

        await CreateService().DeleteAsync(_userId, posting.Id, CancellationToken.None);

        deleted.ShouldNotBeNull();
        deleted.Id.ShouldBe(posting.Id);
    }

    [Fact]
    [Trait("Rule", "BR-07")]
    public async Task DeleteAsync_WhenOpen_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(_userId);
        posting.Publish(Now);

        var delete = () => CreateService().DeleteAsync(_userId, posting.Id, CancellationToken.None);

        await delete.ShouldThrowAsync<InvalidStatusTransitionException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task DeleteAsync_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());

        var delete = () => CreateService().DeleteAsync(_userId, posting.Id, CancellationToken.None);

        await delete.ShouldThrowAsync<NotFoundException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    #endregion

    #region Publish

    [Fact]
    [Trait("Requirement", "FR-08")]
    public async Task PublishAsync_WhenDraftOwnedByCaller_ReturnsAndSavesOpenPosting()
    {
        var posting = StoredPosting(_userId);
        JobPosting? saved = null;
        _repository
            .When(r => r.UpdateAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobPosting>());

        var result = await CreateService().PublishAsync(_userId, posting.Id, CancellationToken.None);

        result.Status.ShouldBe(JobPostingStatus.Open);
        saved.ShouldNotBeNull();
        saved.Status.ShouldBe(JobPostingStatus.Open);
    }

    [Fact]
    [Trait("Rule", "BR-05")]
    public async Task PublishAsync_WhenClosingDateIsToday_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(_userId, closingDate: Today);

        var publish = () => CreateService().PublishAsync(_userId, posting.Id, CancellationToken.None);

        await publish.ShouldThrowAsync<InvalidStatusTransitionException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task PublishAsync_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());

        var publish = () => CreateService().PublishAsync(_userId, posting.Id, CancellationToken.None);

        await publish.ShouldThrowAsync<NotFoundException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    #endregion

    #region Close

    [Fact]
    [Trait("Requirement", "FR-09")]
    public async Task CloseAsync_WhenOpenOwnedByCaller_ReturnsAndSavesClosedPosting()
    {
        var posting = StoredPosting(_userId);
        posting.Publish(Now);
        JobPosting? saved = null;
        _repository
            .When(r => r.UpdateAsync(Arg.Any<JobPosting>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobPosting>());

        var result = await CreateService().CloseAsync(_userId, posting.Id, CancellationToken.None);

        result.Status.ShouldBe(JobPostingStatus.Closed);
        saved.ShouldNotBeNull();
        saved.Status.ShouldBe(JobPostingStatus.Closed);
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task CloseAsync_WhenDraft_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(_userId);

        var close = () => CreateService().CloseAsync(_userId, posting.Id, CancellationToken.None);

        await close.ShouldThrowAsync<InvalidStatusTransitionException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task CloseAsync_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var posting = StoredPosting(Guid.NewGuid());
        posting.Publish(Now);

        var close = () => CreateService().CloseAsync(_userId, posting.Id, CancellationToken.None);

        await close.ShouldThrowAsync<NotFoundException>();

        await ShouldNotHaveSavedAnythingAsync();
    }

    #endregion

    #region Public list (FR-11)

    // Which postings are public (Open, closing today or later) is decided in SQL and covered by the
    // integration tests in JobPostingRepositoryTests; this test proves "today" comes from TimeProvider.
    [Fact]
    [Trait("Requirement", "FR-11")]
    public async Task ListPublicAsync_AsksForPostingsClosingTodayOrLaterAndMapsThem()
    {
        var open = StoredPosting(Guid.NewGuid());
        open.Publish(Now);
        _repository.ListOpenAsync(Today, Arg.Any<CancellationToken>()).Returns([open]);

        var result = await CreateService().ListPublicAsync(CancellationToken.None);

        result.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            posting => posting.Id.ShouldBe(open.Id),
            posting => posting.Title.ShouldBe(open.Title),
            posting => posting.ClosingDate.ShouldBe(open.ClosingDate));
    }

    #endregion

    private async Task ShouldNotHaveSavedAnythingAsync()
    {
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    private JobPostingService CreateService() =>
        new(
            new CreateJobPostingInputModelValidator(_timeProvider),
            new UpdateJobPostingInputModelValidator(_timeProvider),
            _repository,
            _users,
            _departments,
            _timeProvider);

    private static User Recruiter(Guid departmentId) =>
        User.CreateStaff("recruiter@cityofexample.gov", "Jane Doe", "hashed-password",
            UserRole.Recruiter, Guid.NewGuid(), departmentId, Now);

    private JobPosting StoredPosting(Guid ownerId, DateOnly? closingDate = null)
    {
        var posting = JobPosting.Create(
            createdByUserId: ownerId,
            title: "Park Ranger",
            departmentId: _department.Id,
            department: _department.Name,
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.FullTime,
            salaryMin: 50_000m,
            salaryMax: 60_000m,
            closingDate: closingDate ?? new DateOnly(2026, 12, 31),
            createdAt: Now);

        _repository.GetByIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(posting);

        return posting;
    }

    private static CreateJobPostingInputModel ValidInput() => new()
    {
        Title = "Park Ranger",
        Description = "Patrol city parks and assist visitors.",
        Location = "Central Park District",
        EmploymentType = EmploymentType.PartTime,
        SalaryMin = 50_000m,
        SalaryMax = 60_000m,
        ClosingDate = new DateOnly(2026, 12, 31)
    };

    private static UpdateJobPostingInputModel ValidUpdateInput() => new()
    {
        Title = "Senior Park Ranger",
        Description = "Lead the ranger team.",
        Location = "Central Park District",
        EmploymentType = EmploymentType.FullTime,
        SalaryMin = 60_000m,
        SalaryMax = 75_000m,
        ClosingDate = new DateOnly(2026, 12, 31)
    };
}

using CivicJobs.Application.Abstractions;
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

public class JobApplicationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IJobApplicationRepository _applications = Substitute.For<IJobApplicationRepository>();
    private readonly IJobPostingRepository _postings = Substitute.For<IJobPostingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly User _candidate = User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now);
    private readonly Guid _candidateId;

    public JobApplicationServiceTests()
    {
        _candidateId = _candidate.Id;
        _users.GetByIdAsync(_candidateId, Arg.Any<CancellationToken>()).Returns(_candidate);
    }

    #region Apply

    [Fact]
    [Trait("Requirement", "FR-18")]
    public async Task ApplyAsync_ToOpenPosting_SavesSubmittedApplicationOfTheCaller()
    {
        var posting = StoredPosting(JobPostingStatus.Open);
        JobApplication? saved = null;
        _applications
            .When(r => r.AddAsync(Arg.Any<JobApplication>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<JobApplication>());

        var result = await CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.CandidateId.ShouldBe(_candidateId),
            () => saved.JobPostingId.ShouldBe(posting.Id),
            () => saved.CoverLetter.ShouldBe("I love parks."));
        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(saved.Id),
            () => result.JobTitle.ShouldBe("Park Ranger"),
            () => result.Department.ShouldBe("Parks & Recreation"),
            () => result.Status.ShouldBe(JobApplicationStatus.Submitted),
            () => result.AppliedAt.ShouldBe(Now));
    }

    [Theory]
    [Trait("Rule", "BR-18")]
    [InlineData(JobPostingStatus.Draft)]
    [InlineData(JobPostingStatus.PendingApproval)]
    public async Task ApplyAsync_ToPostingThatIsNotPublic_ThrowsNotFoundException(JobPostingStatus status)
    {
        var posting = StoredPosting(status);

        var apply = () => CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        await apply.ShouldThrowAsync<NotFoundException>();
        await _applications.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task ApplyAsync_ToUnknownPosting_ThrowsNotFoundException()
    {
        var apply = () => CreateService().ApplyAsync(_candidateId, Input(Guid.NewGuid()), CancellationToken.None);

        (await apply.ShouldThrowAsync<NotFoundException>())
            .Message.ShouldBe("We couldn't find that position. It may have been removed.");
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task ApplyAsync_ToClosedPosting_ThrowsInvalidStatusTransitionException()
    {
        var posting = StoredPosting(JobPostingStatus.Closed);

        var apply = () => CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        await apply.ShouldThrowAsync<InvalidStatusTransitionException>();
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task ApplyAsync_WhenAlreadyApplied_ThrowsConflictException()
    {
        var posting = StoredPosting(JobPostingStatus.Open);
        _applications.ExistsAsync(posting.Id, _candidateId, Arg.Any<CancellationToken>()).Returns(true);

        var apply = () => CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        (await apply.ShouldThrowAsync<ConflictException>())
            .Message.ShouldBe("You already applied to this position. Check My applications for its status.");
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public async Task ApplyAsync_WithCoverLetterTooLong_ThrowsValidationException()
    {
        var posting = StoredPosting(JobPostingStatus.Open);
        var input = Input(posting.Id);
        input.CoverLetter = new string('a', 2001);

        var apply = () => CreateService().ApplyAsync(_candidateId, input, CancellationToken.None);

        (await apply.ShouldThrowAsync<ValidationException>())
            .Errors.ShouldContain(e => e.PropertyName == nameof(ApplyInputModel.CoverLetter));
    }

    #endregion

    #region Notifications (FR-23)

    [Fact]
    [Trait("Requirement", "FR-23")]
    public async Task ApplyAsync_ToOpenPosting_EmailsTheCandidateAConfirmation()
    {
        var posting = StoredPosting(JobPostingStatus.Open);

        await CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            "candidate@example.com",
            "We received your application for Park Ranger",
            Arg.Is<string>(body => body.Contains("Alex Kim") && body.Contains("Parks & Recreation")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Requirement", "FR-23")]
    public async Task ApplyAsync_WhenApplicationIsNotSaved_SendsNoEmail()
    {
        var posting = StoredPosting(JobPostingStatus.Open);
        _applications.ExistsAsync(posting.Id, _candidateId, Arg.Any<CancellationToken>()).Returns(true);

        var apply = () => CreateService().ApplyAsync(_candidateId, Input(posting.Id), CancellationToken.None);

        await apply.ShouldThrowAsync<ConflictException>();
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    #endregion

    #region My applications

    [Fact]
    [Trait("Requirement", "FR-19")]
    public async Task ListMineAsync_ReturnsApplicationsWithPostingTitleAndStatus()
    {
        var posting = StoredPosting(JobPostingStatus.Open);
        var application = JobApplication.Submit(posting, _candidateId, null, Now);
        application.ChangeStatus(JobApplicationStatus.UnderReview, Now.AddHours(2));
        _applications.ListByCandidateAsync(_candidateId, Arg.Any<CancellationToken>()).Returns([application]);
        _postings.ListByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(posting.Id)), Arg.Any<CancellationToken>())
            .Returns([posting]);

        var result = await CreateService().ListMineAsync(_candidateId, CancellationToken.None);

        result.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            a => a.JobPostingId.ShouldBe(posting.Id),
            a => a.JobTitle.ShouldBe("Park Ranger"),
            a => a.Location.ShouldBe("Central Park District"),
            a => a.Status.ShouldBe(JobApplicationStatus.UnderReview),
            a => a.UpdatedAt.ShouldBe(Now.AddHours(2)));
    }

    #endregion

    private JobApplicationService CreateService() =>
        new(new ApplyInputModelValidator(), _applications, _postings, _users, _emailSender, _timeProvider);

    private static ApplyInputModel Input(Guid postingId) => new() { JobPostingId = postingId, CoverLetter = "I love parks." };

    private JobPosting StoredPosting(JobPostingStatus status)
    {
        var createdAt = Now.AddDays(-5);
        var posting = JobPosting.Create(Guid.NewGuid(), Guid.NewGuid(), "Parks & Recreation", "Park Ranger",
            "Patrol city parks.", "Central Park District", EmploymentType.FullTime, 50_000m, 60_000m, Today.AddDays(30), createdAt);

        if (status != JobPostingStatus.Draft)
        {
            posting.SubmitForApproval(createdAt);
        }

        if (status is JobPostingStatus.Open or JobPostingStatus.Closed)
        {
            posting.Approve(createdAt);
        }

        if (status == JobPostingStatus.Closed)
        {
            posting.Close(createdAt);
        }

        _postings.GetByIdAsync(posting.Id, Arg.Any<CancellationToken>()).Returns(posting);
        return posting;
    }
}

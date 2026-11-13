using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class JobApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    private readonly Guid _candidateId = Guid.NewGuid();

    #region Submit

    [Fact]
    [Trait("Requirement", "FR-18")]
    public void Submit_ToOpenPosting_CreatesSubmittedApplication()
    {
        var posting = OpenPosting();

        var application = JobApplication.Submit(posting, _candidateId, "  I love parks.  ", Now);

        application.ShouldSatisfyAllConditions(
            () => application.Id.ShouldNotBe(Guid.Empty),
            () => application.JobPostingId.ShouldBe(posting.Id),
            () => application.CandidateId.ShouldBe(_candidateId),
            () => application.CoverLetter.ShouldBe("I love parks."),
            () => application.Status.ShouldBe(JobApplicationStatus.Submitted),
            () => application.CreatedAt.ShouldBe(Now),
            () => application.UpdatedAt.ShouldBe(Now));
    }

    [Theory]
    [Trait("Rule", "BR-20")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Submit_WithoutCoverLetter_StoresNoCoverLetter(string? coverLetter)
    {
        var application = JobApplication.Submit(OpenPosting(), _candidateId, coverLetter, Now);

        application.CoverLetter.ShouldBeNull();
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void Submit_WithCoverLetterLongerThan2000Chars_ThrowsDomainValidationException()
    {
        var submit = () => JobApplication.Submit(OpenPosting(), _candidateId, new string('a', 2001), Now);

        submit.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public void Submit_ToPostingClosingToday_CreatesApplication()
    {
        var posting = OpenPosting(closingDate: Today);

        var application = JobApplication.Submit(posting, _candidateId, null, Now);

        application.Status.ShouldBe(JobApplicationStatus.Submitted);
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public void Submit_ToOpenPostingPastItsClosingDate_ThrowsInvalidStatusTransitionException()
    {
        var posting = OpenPosting(closingDate: Today.AddDays(-1));

        var submit = () => JobApplication.Submit(posting, _candidateId, null, Now);

        submit.ShouldThrow<InvalidStatusTransitionException>()
            .Message.ShouldBe("This position is no longer taking applications.");
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public void Submit_ToClosedPosting_ThrowsInvalidStatusTransitionException()
    {
        var posting = OpenPosting();
        posting.Close(Now);

        var submit = () => JobApplication.Submit(posting, _candidateId, null, Now);

        submit.ShouldThrow<InvalidStatusTransitionException>();
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public void Submit_WithoutCandidate_ThrowsDomainValidationException()
    {
        var submit = () => JobApplication.Submit(OpenPosting(), Guid.Empty, null, Now);

        submit.ShouldThrow<DomainValidationException>();
    }

    #endregion

    #region Change status

    [Theory]
    [Trait("Requirement", "FR-21")]
    [Trait("Rule", "BR-19")]
    [InlineData(JobApplicationStatus.Submitted, JobApplicationStatus.UnderReview)]
    [InlineData(JobApplicationStatus.Submitted, JobApplicationStatus.Rejected)]
    [InlineData(JobApplicationStatus.UnderReview, JobApplicationStatus.Hired)]
    [InlineData(JobApplicationStatus.UnderReview, JobApplicationStatus.Rejected)]
    public void ChangeStatus_AllowedTransition_SetsStatusAndUpdatedAt(JobApplicationStatus from, JobApplicationStatus to)
    {
        var application = ApplicationWithStatus(from);

        application.ChangeStatus(to, Later);

        application.Status.ShouldBe(to);
        application.UpdatedAt.ShouldBe(Later);
    }

    [Theory]
    [Trait("Rule", "BR-19")]
    [InlineData(JobApplicationStatus.Submitted, JobApplicationStatus.Hired)]
    [InlineData(JobApplicationStatus.Submitted, JobApplicationStatus.Submitted)]
    [InlineData(JobApplicationStatus.UnderReview, JobApplicationStatus.Submitted)]
    [InlineData(JobApplicationStatus.UnderReview, JobApplicationStatus.UnderReview)]
    [InlineData(JobApplicationStatus.Hired, JobApplicationStatus.Rejected)]
    [InlineData(JobApplicationStatus.Rejected, JobApplicationStatus.UnderReview)]
    public void ChangeStatus_TransitionNotAllowed_ThrowsInvalidStatusTransitionException(
        JobApplicationStatus from,
        JobApplicationStatus to)
    {
        var application = ApplicationWithStatus(from);

        var change = () => application.ChangeStatus(to, Later);

        change.ShouldThrow<InvalidStatusTransitionException>();
        application.Status.ShouldBe(from);
    }

    [Theory]
    [Trait("Rule", "BR-19")]
    [InlineData(JobApplicationStatus.Hired, "This application already has a final decision.")]
    [InlineData(JobApplicationStatus.Rejected, "This application already has a final decision.")]
    public void ChangeStatus_WhenFinal_ExplainsWhy(JobApplicationStatus from, string message)
    {
        var application = ApplicationWithStatus(from);

        var change = () => application.ChangeStatus(JobApplicationStatus.UnderReview, Later);

        change.ShouldThrow<InvalidStatusTransitionException>().Message.ShouldBe(message);
    }

    #endregion

    private JobApplication ApplicationWithStatus(JobApplicationStatus status)
    {
        var application = JobApplication.Submit(OpenPosting(), _candidateId, null, Now);

        if (status is JobApplicationStatus.UnderReview or JobApplicationStatus.Hired)
        {
            application.ChangeStatus(JobApplicationStatus.UnderReview, Now);
        }

        if (status != JobApplicationStatus.Submitted && status != JobApplicationStatus.UnderReview)
        {
            application.ChangeStatus(status, Now);
        }

        return application;
    }

    /// <summary>An Open posting, created and approved a month before the closing date so the rules allow it.</summary>
    private static JobPosting OpenPosting(DateOnly? closingDate = null)
    {
        var closing = closingDate ?? Today.AddDays(30);
        var createdAt = new DateTimeOffset(closing.AddDays(-30).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var posting = JobPosting.Create(Guid.NewGuid(), Guid.NewGuid(), "Parks & Recreation", "Park Ranger",
            "Patrol city parks.", "Central Park District", EmploymentType.FullTime, 50_000m, 60_000m, closing, createdAt);
        posting.SubmitForApproval(createdAt);
        posting.Approve(createdAt);
        return posting;
    }
}

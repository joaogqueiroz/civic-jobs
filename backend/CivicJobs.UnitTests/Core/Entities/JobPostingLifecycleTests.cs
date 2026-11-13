using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class JobPostingLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    #region Publish

    [Fact]
    [Trait("Requirement", "FR-08")]
    public void Publish_WhenDraft_SetsStatusToOpenAndUpdatedAt()
    {
        var posting = DraftPosting();

        posting.Publish(Later);

        posting.ShouldSatisfyAllConditions(
            () => posting.Status.ShouldBe(JobPostingStatus.Open),
            () => posting.UpdatedAt.ShouldBe(Later));
    }

    [Fact]
    [Trait("Rule", "BR-05")]
    public void Publish_WhenClosingDateIsTomorrow_SetsStatusToOpen()
    {
        var posting = DraftPosting(closingDate: Today.AddDays(1));

        posting.Publish(Now);

        posting.Status.ShouldBe(JobPostingStatus.Open);
    }

    [Fact]
    [Trait("Rule", "BR-05")]
    public void Publish_WhenClosingDateIsToday_ThrowsInvalidStatusTransitionException()
    {
        var posting = DraftPosting(closingDate: Today);

        var publish = () => posting.Publish(Now);

        publish.ShouldThrow<InvalidStatusTransitionException>()
            .Message.ShouldBe("Choose a closing date after today. Open postings must close in the future.");
        posting.Status.ShouldBe(JobPostingStatus.Draft);
    }

    [Fact]
    [Trait("Rule", "BR-05")]
    public void Publish_WhenClosingDateIsInThePast_ThrowsInvalidStatusTransitionException()
    {
        var posting = DraftPosting(closingDate: Today);

        var publish = () => posting.Publish(Now.AddDays(2));

        publish.ShouldThrow<InvalidStatusTransitionException>();
    }

    [Theory]
    [Trait("Rule", "BR-06")]
    [InlineData(JobPostingStatus.Open, "This posting is already published.")]
    [InlineData(JobPostingStatus.Closed, "This posting is closed and can't be published again.")]
    public void Publish_WhenNotDraft_ThrowsInvalidStatusTransitionException(JobPostingStatus status, string message)
    {
        var posting = PostingWithStatus(status);

        var publish = () => posting.Publish(Later);

        publish.ShouldThrow<InvalidStatusTransitionException>().Message.ShouldBe(message);
        posting.Status.ShouldBe(status);
    }

    #endregion

    #region Close

    [Fact]
    [Trait("Requirement", "FR-09")]
    public void Close_WhenOpen_SetsStatusToClosedAndUpdatedAt()
    {
        var posting = PostingWithStatus(JobPostingStatus.Open);
        var closedAt = Later.AddHours(1);

        posting.Close(closedAt);

        posting.ShouldSatisfyAllConditions(
            () => posting.Status.ShouldBe(JobPostingStatus.Closed),
            () => posting.UpdatedAt.ShouldBe(closedAt));
    }

    [Theory]
    [Trait("Rule", "BR-06")]
    [InlineData(JobPostingStatus.Draft, "Publish this draft before closing it.")]
    [InlineData(JobPostingStatus.Closed, "This posting is already closed.")]
    public void Close_WhenNotOpen_ThrowsInvalidStatusTransitionException(JobPostingStatus status, string message)
    {
        var posting = PostingWithStatus(status);

        var close = () => posting.Close(Later);

        close.ShouldThrow<InvalidStatusTransitionException>().Message.ShouldBe(message);
        posting.Status.ShouldBe(status);
    }

    #endregion

    #region Delete

    [Fact]
    [Trait("Requirement", "FR-10")]
    public void EnsureCanBeDeleted_WhenDraft_DoesNotThrow()
    {
        var posting = DraftPosting();

        Should.NotThrow(posting.EnsureCanBeDeleted);
    }

    [Theory]
    [Trait("Rule", "BR-07")]
    [InlineData(JobPostingStatus.Open)]
    [InlineData(JobPostingStatus.Closed)]
    public void EnsureCanBeDeleted_WhenNotDraft_ThrowsInvalidStatusTransitionException(JobPostingStatus status)
    {
        var posting = PostingWithStatus(status);

        Should.Throw<InvalidStatusTransitionException>(posting.EnsureCanBeDeleted)
            .Message.ShouldBe("Only drafts can be deleted. Published postings are kept as a public record.");
    }

    #endregion

    #region Update

    [Fact]
    [Trait("Requirement", "FR-07")]
    public void Update_WhenDraft_ChangesDetailsAndUpdatedAt()
    {
        var posting = DraftPosting();
        var ownerId = posting.CreatedByUserId;

        posting.Update(
            title: "Senior Park Ranger",
            description: "Lead the ranger team.",
            location: "North District",
            employmentType: EmploymentType.Temporary,
            salaryMin: 70_000m,
            salaryMax: 80_000m,
            closingDate: new DateOnly(2027, 1, 31),
            updatedAt: Later);

        posting.ShouldSatisfyAllConditions(
            () => posting.Title.ShouldBe("Senior Park Ranger"),
            () => posting.Department.ShouldBe("Parks & Recreation"),
            () => posting.Description.ShouldBe("Lead the ranger team."),
            () => posting.Location.ShouldBe("North District"),
            () => posting.EmploymentType.ShouldBe(EmploymentType.Temporary),
            () => posting.SalaryMin.ShouldBe(70_000m),
            () => posting.SalaryMax.ShouldBe(80_000m),
            () => posting.ClosingDate.ShouldBe(new DateOnly(2027, 1, 31)),
            () => posting.UpdatedAt.ShouldBe(Later),
            () => posting.CreatedAt.ShouldBe(Now),
            () => posting.CreatedByUserId.ShouldBe(ownerId),
            () => posting.Status.ShouldBe(JobPostingStatus.Draft));
    }

    [Fact]
    [Trait("Requirement", "FR-07")]
    public void Update_WhenOpenAndClosingDateIsAfterToday_ChangesDetails()
    {
        var posting = PostingWithStatus(JobPostingStatus.Open);

        Update(posting, title: "Senior Park Ranger", closingDate: Today.AddDays(1));

        posting.ShouldSatisfyAllConditions(
            () => posting.Title.ShouldBe("Senior Park Ranger"),
            () => posting.Status.ShouldBe(JobPostingStatus.Open));
    }

    [Theory]
    [Trait("Requirement", "FR-07")]
    [InlineData(JobPostingStatus.Draft)]
    [InlineData(JobPostingStatus.Open)]
    public void EnsureCanBeEdited_WhenDraftOrOpen_DoesNotThrow(JobPostingStatus status)
    {
        var posting = PostingWithStatus(status);

        Should.NotThrow(posting.EnsureCanBeEdited);
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public void EnsureCanBeEdited_WhenClosed_ThrowsInvalidStatusTransitionException()
    {
        var posting = PostingWithStatus(JobPostingStatus.Closed);

        Should.Throw<InvalidStatusTransitionException>(posting.EnsureCanBeEdited)
            .Message.ShouldBe("This posting is closed, so it can't be edited.");
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public void Update_WhenClosed_ThrowsInvalidStatusTransitionException()
    {
        var posting = PostingWithStatus(JobPostingStatus.Closed);

        var update = () => Update(posting, title: "Senior Park Ranger");

        update.ShouldThrow<InvalidStatusTransitionException>();
        posting.Title.ShouldBe("Park Ranger");
    }

    [Fact]
    [Trait("Rule", "BR-05")]
    public void Update_WhenOpenAndClosingDateIsToday_ThrowsInvalidStatusTransitionException()
    {
        var posting = PostingWithStatus(JobPostingStatus.Open);

        var update = () => Update(posting, closingDate: Today);

        update.ShouldThrow<InvalidStatusTransitionException>();
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Update_WhenDraftAndClosingDateIsToday_ChangesClosingDate()
    {
        var posting = DraftPosting();

        Update(posting, closingDate: Today);

        posting.ClosingDate.ShouldBe(Today);
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Update_WhenDraftAndClosingDateIsInThePast_ThrowsDomainValidationException()
    {
        var posting = DraftPosting();

        var update = () => Update(posting, closingDate: Today.AddDays(-1));

        update.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData("title", null)]
    [InlineData("title", "   ")]
    [InlineData("description", "")]
    [InlineData("location", "")]
    public void Update_WhenRequiredFieldIsMissing_ThrowsDomainValidationException(string field, string? value)
    {
        var posting = DraftPosting();

        var update = () => Update(
            posting,
            title: field == "title" ? value! : "Park Ranger",
            description: field == "description" ? value! : "Patrol city parks.",
            location: field == "location" ? value! : "Central Park District");

        update.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-04")]
    [InlineData(0, 60_000)]
    [InlineData(50_000, 49_999)]
    public void Update_WhenSalaryRangeIsInvalid_ThrowsDomainValidationException(int salaryMin, int salaryMax)
    {
        var posting = DraftPosting();

        var update = () => Update(posting, salaryMin: salaryMin, salaryMax: salaryMax);

        update.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-10")]
    public void Update_WhenEmploymentTypeIsNotDefined_ThrowsDomainValidationException()
    {
        var posting = DraftPosting();

        var update = () => Update(posting, employmentType: (EmploymentType)99);

        update.ShouldThrow<DomainValidationException>();
    }

    #endregion

    private static JobPosting DraftPosting(DateOnly? closingDate = null) =>
        JobPosting.Create(
            createdByUserId: Guid.NewGuid(),
            title: "Park Ranger",
            departmentId: Guid.NewGuid(),
            department: "Parks & Recreation",
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.FullTime,
            salaryMin: 50_000m,
            salaryMax: 60_000m,
            closingDate: closingDate ?? new DateOnly(2026, 12, 31),
            createdAt: Now);

    private static JobPosting PostingWithStatus(JobPostingStatus status)
    {
        var posting = DraftPosting();

        if (status is JobPostingStatus.Open or JobPostingStatus.Closed)
        {
            posting.Publish(Now);
        }

        if (status is JobPostingStatus.Closed)
        {
            posting.Close(Now);
        }

        return posting;
    }

    private static void Update(
        JobPosting posting,
        string title = "Park Ranger",
        string description = "Patrol city parks.",
        string location = "Central Park District",
        EmploymentType employmentType = EmploymentType.FullTime,
        decimal salaryMin = 50_000m,
        decimal salaryMax = 60_000m,
        DateOnly? closingDate = null) =>
        posting.Update(
            title,
            description,
            location,
            employmentType,
            salaryMin,
            salaryMax,
            closingDate ?? new DateOnly(2026, 12, 31),
            Later);
}

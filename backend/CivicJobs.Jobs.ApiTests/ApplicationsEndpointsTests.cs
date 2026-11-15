using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using Shouldly;

namespace CivicJobs.Jobs.ApiTests;

public sealed class ApplicationsEndpointsTests(JobsApiFactory factory) : IClassFixture<JobsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

    [Fact]
    [Trait("Requirement", "FR-18")]
    [Trait("Requirement", "FR-19")]
    public async Task Apply_AsCandidateToOpenPosting_Returns201AndShowsInMyApplications()
    {
        var (_, _, postingId) = await OpenPostingAsync();
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);

        var apply = await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId, coverLetter = "I love parks." });

        apply.StatusCode.ShouldBe(HttpStatusCode.Created);
        var mine = await candidate.GetFromJsonAsync<List<CandidateApplicationViewModel>>("/api/applications", Json);
        mine!.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            a => a.JobPostingId.ShouldBe(postingId),
            a => a.JobTitle.ShouldBe("Park Ranger"),
            a => a.Status.ShouldBe(JobApplicationStatus.Submitted));
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task Apply_TwiceToTheSamePosting_Returns409()
    {
        var (_, _, postingId) = await OpenPostingAsync();
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);
        (await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId });

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public async Task Apply_ToDraftPosting_Returns404()
    {
        var recruiter = await factory.CreateClientForNewRecruiterAsync();
        var draft = await CreatePostingAsync(recruiter);
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);

        var response = await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = draft });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public async Task Apply_WithCoverLetterOverTheLimit_Returns400WithCoverLetterError()
    {
        var (_, _, postingId) = await OpenPostingAsync();
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);

        var response = await candidate.PostAsJsonAsync("/api/applications",
            new { jobPostingId = postingId, coverLetter = new string('a', 2001) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("coverLetter", out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Rule", "BR-12")]
    public async Task Apply_AsRecruiter_Returns403()
    {
        var (recruiter, _, postingId) = await OpenPostingAsync();

        var response = await recruiter.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Requirement", "FR-20")]
    [Trait("Requirement", "FR-21")]
    public async Task ReviewApplication_FullFlow_CandidateSeesEachDecision()
    {
        var (_, manager, postingId) = await OpenPostingAsync();
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);
        await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId, coverLetter = "I love parks." });

        var applications = await manager.GetFromJsonAsync<List<PostingApplicationViewModel>>(
            $"/api/department/job-postings/{postingId}/applications", Json);
        var application = applications!.ShouldHaveSingleItem();
        application.CoverLetter.ShouldBe("I love parks.");

        var review = await manager.PutAsJsonAsync($"/api/department/applications/{application.Id}/status", new { status = "UnderReview" });
        var hire = await manager.PutAsJsonAsync($"/api/department/applications/{application.Id}/status", new { status = "Hired" });
        var rejectAfterHire = await manager.PutAsJsonAsync($"/api/department/applications/{application.Id}/status", new { status = "Rejected" });

        review.StatusCode.ShouldBe(HttpStatusCode.OK);
        hire.StatusCode.ShouldBe(HttpStatusCode.OK);
        rejectAfterHire.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var mine = await candidate.GetFromJsonAsync<List<CandidateApplicationViewModel>>("/api/applications", Json);
        mine!.Single().Status.ShouldBe(JobApplicationStatus.Hired);
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ChangeApplicationStatus_AsManagerOfAnotherDepartment_Returns404()
    {
        var (_, manager, postingId) = await OpenPostingAsync();
        var otherManager = await factory.CreateClientForAsync(UserRole.HiringManager);
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);
        await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId });
        var application = (await manager.GetFromJsonAsync<List<PostingApplicationViewModel>>(
            $"/api/department/job-postings/{postingId}/applications", Json))!.Single();

        var response = await otherManager.PutAsJsonAsync($"/api/department/applications/{application.Id}/status", new { status = "Rejected" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Rule", "BR-19")]
    public async Task ChangeApplicationStatus_FromSubmittedStraightToHired_Returns409()
    {
        var (_, manager, postingId) = await OpenPostingAsync();
        var candidate = await factory.CreateClientForAsync(UserRole.Candidate);
        await candidate.PostAsJsonAsync("/api/applications", new { jobPostingId = postingId });
        var application = (await manager.GetFromJsonAsync<List<PostingApplicationViewModel>>(
            $"/api/department/job-postings/{postingId}/applications", Json))!.Single();

        var response = await manager.PutAsJsonAsync($"/api/department/applications/{application.Id}/status", new { status = "Hired" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>A recruiter and a hiring manager of a new department, and an Open posting of theirs.</summary>
    private async Task<(HttpClient Recruiter, HttpClient Manager, Guid PostingId)> OpenPostingAsync()
    {
        var department = await factory.CreateDepartmentAsync();
        var recruiter = await factory.CreateClientForAsync(UserRole.Recruiter, department);
        var manager = await factory.CreateClientForAsync(UserRole.HiringManager, department);
        var postingId = await CreatePostingAsync(recruiter);
        (await recruiter.PostAsync($"/api/job-postings/{postingId}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await manager.PostAsync($"/api/department/job-postings/{postingId}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        return (recruiter, manager, postingId);
    }

    private static async Task<Guid> CreatePostingAsync(HttpClient recruiter)
    {
        var response = await recruiter.PostAsJsonAsync("/api/job-postings", new
        {
            title = "Park Ranger",
            description = "Patrol city parks and assist visitors.",
            location = "Central Park District",
            employmentType = "FullTime",
            salaryMin = 50_000,
            salaryMax = 60_000,
            closingDate = Today.AddDays(30)
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JobPostingViewModel>(Json))!.Id;
    }
}

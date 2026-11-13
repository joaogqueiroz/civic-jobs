using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using Shouldly;

namespace CivicJobs.Jobs.ApiTests;

public sealed class JobPostingsEndpointsTests(JobsApiFactory factory) : IClassFixture<JobsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

    [Fact]
    [Trait("Requirement", "FR-05")]
    public async Task ListPostings_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/job-postings");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [Trait("Rule", "BR-12")]
    [InlineData(UserRole.Candidate)]
    [InlineData(UserRole.HiringManager)]
    [InlineData(UserRole.Admin)]
    public async Task ListPostings_WithRoleOtherThanRecruiter_Returns403(UserRole role)
    {
        var client = await factory.CreateClientForAsync(role);

        var response = await client.GetAsync("/api/job-postings");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    [Trait("Rule", "BR-13")]
    public async Task CreatePosting_AsRecruiter_TakesDepartmentFromTheAccount()
    {
        var department = await factory.CreateDepartmentAsync("Animal Services");
        var client = await factory.CreateClientForAsync(UserRole.Recruiter, department);

        var posting = await CreatePostingAsync(client);

        posting.Department.ShouldBe("Animal Services");
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public async Task GetPosting_OwnedByAnotherRecruiter_Returns404()
    {
        var owner = await factory.CreateClientForNewRecruiterAsync();
        var otherRecruiter = await factory.CreateClientForNewRecruiterAsync();
        var created = await CreatePostingAsync(owner);

        var response = await otherRecruiter.GetAsync($"/api/job-postings/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public async Task CreatePosting_WithSalaryMaxBelowSalaryMin_Returns400WithSalaryMaxError()
    {
        var client = await factory.CreateClientForNewRecruiterAsync();

        var response = await client.PostAsJsonAsync("/api/job-postings", NewPosting(salaryMin: 60_000, salaryMax: 50_000));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("salaryMax", out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    [Trait("Requirement", "FR-08")]
    [Trait("Requirement", "FR-16")]
    [Trait("Requirement", "FR-09")]
    public async Task CreateSubmitApproveClose_FullFlow_MovesPostingFromDraftToClosed()
    {
        var department = await factory.CreateDepartmentAsync();
        var recruiter = await factory.CreateClientForAsync(UserRole.Recruiter, department);
        var manager = await factory.CreateClientForAsync(UserRole.HiringManager, department);

        var create = await recruiter.PostAsJsonAsync("/api/job-postings", NewPosting());
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        create.Headers.Location.ShouldNotBeNull();
        var draft = await ReadPostingAsync(create);
        draft.Status.ShouldBe(JobPostingStatus.Draft);

        var submit = await recruiter.PostAsync($"/api/job-postings/{draft.Id}/submit", null);
        submit.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(submit)).Status.ShouldBe(JobPostingStatus.PendingApproval);

        var approve = await manager.PostAsync($"/api/department/job-postings/{draft.Id}/approve", null);
        approve.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(approve)).Status.ShouldBe(JobPostingStatus.Open);

        var close = await recruiter.PostAsync($"/api/job-postings/{draft.Id}/close", null);
        close.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(close)).Status.ShouldBe(JobPostingStatus.Closed);

        var get = await recruiter.GetAsync(create.Headers.Location);
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(get)).Status.ShouldBe(JobPostingStatus.Closed);
    }

    [Fact]
    [Trait("Requirement", "FR-17")]
    public async Task RejectPosting_WithReason_ReturnsItToDraftWithTheReasonForTheRecruiter()
    {
        var department = await factory.CreateDepartmentAsync();
        var recruiter = await factory.CreateClientForAsync(UserRole.Recruiter, department);
        var manager = await factory.CreateClientForAsync(UserRole.HiringManager, department);
        var posting = await CreatePostingAsync(recruiter);
        (await recruiter.PostAsync($"/api/job-postings/{posting.Id}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var reject = await manager.PostAsJsonAsync($"/api/department/job-postings/{posting.Id}/reject",
            new { reason = "Add the shift hours." });

        reject.StatusCode.ShouldBe(HttpStatusCode.OK);
        var seenByRecruiter = await ReadPostingAsync(await recruiter.GetAsync($"/api/job-postings/{posting.Id}"));
        seenByRecruiter.Status.ShouldBe(JobPostingStatus.Draft);
        seenByRecruiter.RejectionReason.ShouldBe("Add the shift hours.");
    }

    [Fact]
    [Trait("Rule", "BR-17")]
    public async Task RejectPosting_WithoutReason_Returns400WithReasonError()
    {
        var department = await factory.CreateDepartmentAsync();
        var recruiter = await factory.CreateClientForAsync(UserRole.Recruiter, department);
        var manager = await factory.CreateClientForAsync(UserRole.HiringManager, department);
        var posting = await CreatePostingAsync(recruiter);
        await recruiter.PostAsync($"/api/job-postings/{posting.Id}/submit", null);

        var response = await manager.PostAsJsonAsync($"/api/department/job-postings/{posting.Id}/reject", new { reason = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("reason", out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Rule", "BR-16")]
    public async Task ApprovePosting_AsHiringManagerOfAnotherDepartment_Returns404()
    {
        var recruiter = await factory.CreateClientForAsync(UserRole.Recruiter);
        var otherManager = await factory.CreateClientForAsync(UserRole.HiringManager);
        var posting = await CreatePostingAsync(recruiter);
        await recruiter.PostAsync($"/api/job-postings/{posting.Id}/submit", null);

        var response = await otherManager.PostAsync($"/api/department/job-postings/{posting.Id}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Rule", "BR-12")]
    public async Task ApprovePosting_AsRecruiter_Returns403()
    {
        var recruiter = await factory.CreateClientForNewRecruiterAsync();
        var posting = await CreatePostingAsync(recruiter);
        await recruiter.PostAsync($"/api/job-postings/{posting.Id}/submit", null);

        var response = await recruiter.PostAsync($"/api/department/job-postings/{posting.Id}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task UpdatePosting_WhilePendingApproval_Returns409()
    {
        var recruiter = await factory.CreateClientForNewRecruiterAsync();
        var posting = await CreatePostingAsync(recruiter);
        await recruiter.PostAsync($"/api/job-postings/{posting.Id}/submit", null);

        var response = await recruiter.PutAsJsonAsync($"/api/job-postings/{posting.Id}", NewPosting());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Rule", "BR-06")]
    public async Task SubmitPosting_WhenClosed_Returns409ProblemDetails()
    {
        var department = await factory.CreateDepartmentAsync();
        var client = await factory.CreateClientForAsync(UserRole.Recruiter, department);
        var manager = await factory.CreateClientForAsync(UserRole.HiringManager, department);
        var posting = await CreatePostingAsync(client);
        (await client.PostAsync($"/api/job-postings/{posting.Id}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await manager.PostAsync($"/api/department/job-postings/{posting.Id}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync($"/api/job-postings/{posting.Id}/close", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.PostAsync($"/api/job-postings/{posting.Id}/submit", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .ShouldBe("This posting is closed and can't be published again.");
    }

    private static async Task<JobPostingViewModel> CreatePostingAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/job-postings", NewPosting());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await ReadPostingAsync(response);
    }

    private static async Task<JobPostingViewModel> ReadPostingAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JobPostingViewModel>(Json))!;

    private static object NewPosting(decimal salaryMin = 50_000, decimal salaryMax = 60_000) => new
    {
        title = "Park Ranger",
        description = "Patrol city parks and assist visitors.",
        location = "Central Park District",
        employmentType = "FullTime",
        salaryMin,
        salaryMax,
        closingDate = Today.AddDays(30)
    };
}

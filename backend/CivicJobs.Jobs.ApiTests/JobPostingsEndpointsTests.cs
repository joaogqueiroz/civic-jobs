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
    [Trait("Requirement", "FR-09")]
    public async Task CreatePublishClose_FullFlow_MovesPostingFromDraftToOpenToClosed()
    {
        var client = await factory.CreateClientForNewRecruiterAsync();

        var create = await client.PostAsJsonAsync("/api/job-postings", NewPosting());
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        create.Headers.Location.ShouldNotBeNull();
        var draft = await ReadPostingAsync(create);
        draft.Status.ShouldBe(JobPostingStatus.Draft);

        var publish = await client.PostAsync($"/api/job-postings/{draft.Id}/publish", null);
        publish.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(publish)).Status.ShouldBe(JobPostingStatus.Open);

        var close = await client.PostAsync($"/api/job-postings/{draft.Id}/close", null);
        close.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(close)).Status.ShouldBe(JobPostingStatus.Closed);

        var get = await client.GetAsync(create.Headers.Location);
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPostingAsync(get)).Status.ShouldBe(JobPostingStatus.Closed);
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
        department = "Parks & Recreation",
        description = "Patrol city parks and assist visitors.",
        location = "Central Park District",
        employmentType = "FullTime",
        salaryMin,
        salaryMax,
        closingDate = Today.AddDays(30)
    };
}

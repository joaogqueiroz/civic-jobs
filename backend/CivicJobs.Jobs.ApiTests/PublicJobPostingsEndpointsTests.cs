using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace CivicJobs.Jobs.ApiTests;

public sealed class PublicJobPostingsEndpointsTests(JobsApiFactory factory) : IClassFixture<JobsApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

    [Fact]
    [Trait("Requirement", "FR-11")]
    public async Task ListPublicPostings_WithoutToken_ReturnsOpenPostingsButNotDrafts()
    {
        var recruiter = await factory.CreateClientForNewRecruiterAsync();
        var draftId = await CreateAsync(recruiter, "Draft Lifeguard");
        var openId = await CreateAsync(recruiter, "Open Park Ranger");
        (await recruiter.PostAsync($"/api/job-postings/{openId}/publish", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await factory.CreateClient().GetAsync("/api/public/job-postings");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ids = (await response.Content.ReadFromJsonAsync<JsonElement[]>())!
            .Select(posting => posting.GetProperty("id").GetGuid())
            .ToList();
        ids.ShouldContain(openId);
        ids.ShouldNotContain(draftId);
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/job-postings", new
        {
            title,
            department = "Parks & Recreation",
            description = "Patrol city parks and assist visitors.",
            location = "Central Park District",
            employmentType = "FullTime",
            salaryMin = 50_000,
            salaryMax = 60_000,
            closingDate = Today.AddDays(30)
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Api.Development;
using TaskManager.Api.Tests.Infrastructure;

namespace TaskManager.Api.Tests;

public sealed class DevTokenEndpointTests(DevelopmentTaskApiFactory factory) : IClassFixture<DevelopmentTaskApiFactory>
{
    [Fact]
    public async Task Issued_token_authenticates_against_the_api()
    {
        var client = factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync("/dev/token", new { userId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var token = await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var tasks = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, tasks.StatusCode);
    }

    [Fact]
    public async Task Empty_user_id_returns_400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/dev/token", new { userId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_served_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"bearer\"", json);
    }
}

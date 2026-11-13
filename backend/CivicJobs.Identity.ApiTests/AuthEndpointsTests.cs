using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CivicJobs.Application.ViewModels;
using Shouldly;

namespace CivicJobs.Identity.ApiTests;

public sealed class AuthEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Demo@12345";

    private readonly HttpClient _client;

    public AuthEndpointsTests(IdentityApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    [Trait("Requirement", "FR-01")]
    public async Task Register_WithValidInput_Returns201WithUserAndNoPasswordHash()
    {
        var email = UniqueEmail();

        var response = await RegisterAsync(email);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().ShouldBe(email);
        body.GetProperty("role").GetString().ShouldBe("Candidate");
        body.TryGetProperty("passwordHash", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task Login_WithRegisteredCredentials_Returns200WithAccessToken()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<AuthTokenViewModel>();
        token.ShouldNotBeNull();
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetMe_WithTokenFromLogin_Returns200WithCurrentUser()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var token = await login.Content.ReadFromJsonAsync<AuthTokenViewModel>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserViewModel>())!.Email.ShouldBe(email);
    }

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task Register_WithEmailAlreadyInUse_Returns409ProblemDetails()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task Login_WithWrongPassword_Returns401ProblemDetails()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong@12345" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("accessToken", out _).ShouldBeFalse();
    }

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Jane Doe", email, password = Password });

    private static string UniqueEmail() => $"recruiter-{Guid.NewGuid():N}@cityofexample.gov";
}

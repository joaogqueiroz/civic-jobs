using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace CivicJobs.Identity.ApiTests;

public sealed class SessionCookieEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Demo@12345";
    private const string CookieName = "civicjobs_session";

    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    public SessionCookieEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Rule", "BR-21")]
    public async Task Login_SetsHttpOnlySecureStrictSessionCookie()
    {
        var response = await LoginAsync(await RegisterAsync());

        var cookie = SessionCookie(response);
        cookie.ShouldNotBeNull();
        cookie.ShouldSatisfyAllConditions(
            () => cookie.ShouldContain("httponly", Case.Insensitive),
            () => cookie.ShouldContain("secure", Case.Insensitive),
            () => cookie.ShouldContain("samesite=strict", Case.Insensitive),
            () => cookie.ShouldContain("path=/", Case.Insensitive),
            () => cookie.ShouldContain("expires=", Case.Insensitive));
    }

    [Fact]
    [Trait("Rule", "BR-21")]
    public async Task GetMe_WithOnlyTheSessionCookie_Returns200()
    {
        var email = await RegisterAsync();
        var token = CookieValue(SessionCookie(await LoginAsync(email))!);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Cookie", $"{CookieName}={token}");
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Rule", "BR-21")]
    public async Task GetMe_WithATamperedSessionCookie_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Cookie", $"{CookieName}=not-a-real-token");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Requirement", "FR-24")]
    public async Task Logout_Returns204AndExpiresTheSessionCookie()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cookie = SessionCookie(response);
        cookie.ShouldNotBeNull();
        cookie.ShouldContain("expires=Thu, 01 Jan 1970", Case.Insensitive);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"candidate-{Guid.NewGuid():N}@example.com";
        (await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Jane Doe", email, password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        return email;
    }

    private Task<HttpResponseMessage> LoginAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

    private static string? SessionCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0][(CookieName.Length + 1)..];
}

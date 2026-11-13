using Microsoft.AspNetCore.Http;

namespace CivicJobs.Infrastructure.Web;

/// <summary>
/// The access token as a cookie (BR-21): httpOnly so page scripts can't read it, Secure, and SameSite=Strict so
/// other sites can't send it (CSRF). Browsers treat http://localhost as secure, so it also works in development.
/// </summary>
public static class SessionCookie
{
    public const string Name = "civicjobs_session";

    public static void Append(HttpResponse response, string accessToken, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, accessToken, Options(expiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(expiresAt: null));

    private static CookieOptions Options(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = expiresAt,
        IsEssential = true
    };
}

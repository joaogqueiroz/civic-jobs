using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Api.Tests.Infrastructure;

/// <summary>Generates JWTs signed with the test-only settings injected by <see cref="TaskApiFactory"/>.</summary>
public static class TestTokens
{
    public const string Issuer = "TaskManager.Tests";
    public const string Audience = "TaskManager.Tests.Clients";
    public const string SigningKey = "test-only-signing-key-that-is-long-enough-for-hs256";

    public static string Create(Guid userId, string signingKey = SigningKey)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static HttpClient CreateClientFor(this TaskApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Create(userId));
        return client;
    }
}

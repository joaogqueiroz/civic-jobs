using System.Text;
using CivicJobs.Core.Entities;
using CivicJobs.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Auth;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "CivicJobs.Tests",
        Audience = "CivicJobs.Tests.Clients",
        Key = "test-only-signing-key-that-is-long-enough-for-hs256",
        ExpiryMinutes = 60
    };

    private readonly User _user = User.Create("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);
    private readonly JwtTokenService _service = new(Microsoft.Extensions.Options.Options.Create(Options), new FakeTimeProvider(Now));

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void CreateToken_ContainsUserIdEmailAndNameClaims()
    {
        var token = Read(_service.CreateToken(_user).AccessToken);

        token.ShouldSatisfyAllConditions(
            () => token.Subject.ShouldBe(_user.Id.ToString()),
            () => token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(_user.Email),
            () => token.GetClaim(JwtRegisteredClaimNames.Name).Value.ShouldBe(_user.FullName));
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void CreateToken_UsesConfiguredIssuerAndAudience()
    {
        var token = Read(_service.CreateToken(_user).AccessToken);

        token.ShouldSatisfyAllConditions(
            () => token.Issuer.ShouldBe(Options.Issuer),
            () => token.Audiences.ShouldBe([Options.Audience]));
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void CreateToken_ExpiresAfterConfiguredMinutes()
    {
        var result = _service.CreateToken(_user);

        result.ExpiresAt.ShouldBe(Now.AddMinutes(60));
        Read(result.AccessToken).ValidTo.ShouldBe(Now.AddMinutes(60).UtcDateTime);
    }

    [Theory]
    [Trait("Requirement", "FR-02")]
    [InlineData("test-only-signing-key-that-is-long-enough-for-hs256", true)]
    [InlineData("a-different-key-that-is-also-long-enough-for-hs256", false)]
    public async Task CreateToken_IsSignedWithConfiguredKey(string validationKey, bool expectedValid)
    {
        var accessToken = _service.CreateToken(_user).AccessToken;

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(validationKey)),
            ValidateLifetime = false
        });

        result.IsValid.ShouldBe(expectedValid);
    }

    private static JsonWebToken Read(string accessToken) => new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
}

using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Auth;

namespace TaskManager.Api.Development;

/// <summary>Request for a development token.</summary>
/// <param name="UserId">The user id to place in the "sub" claim.</param>
/// <param name="Email">Optional email claim.</param>
public sealed record DevTokenRequest(Guid UserId, string? Email);

public sealed record DevTokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues tokens for local testing from Swagger. Registered and mapped only in the
/// Development environment (see Program.cs), so the route does not exist elsewhere.
/// </summary>
public sealed class DevTokenIssuer(IOptions<JwtOptions> options)
{
    public DevTokenResponse Issue(Guid userId, string? email)
    {
        var jwt = options.Value;
        // Deliberately the system clock, not the injected TimeProvider: the JWT validator checks
        // lifetimes against the system clock, so issuing with any other clock breaks nbf/exp.
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(jwt.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new DevTokenResponse(token, "Bearer", expires);
    }
}

public static class DevTokenEndpoints
{
    public static IServiceCollection AddDevTokenIssuer(this IServiceCollection services) =>
        services.AddSingleton<DevTokenIssuer>();

    public static IEndpointRouteBuilder MapDevTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/token", (DevTokenRequest request, DevTokenIssuer issuer) =>
            {
                if (request.UserId == Guid.Empty)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        [nameof(DevTokenRequest.UserId)] = ["UserId must be a non-empty GUID."]
                    });
                }

                return Results.Ok(issuer.Issue(request.UserId, request.Email));
            })
            .AllowAnonymous()
            .WithTags("Development")
            .WithSummary("Issue a JWT for a user id (Development only).")
            .Produces<DevTokenResponse>()
            .ProducesValidationProblem();

        return app;
    }
}

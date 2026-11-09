using System.Security.Claims;
using CivicJobs.Application.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CivicJobs.Infrastructure.Web;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The caller's user id, taken from the token's "sub" claim (never from the request).</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new UnauthorizedException("Your session has ended. Log in again to continue.");
}

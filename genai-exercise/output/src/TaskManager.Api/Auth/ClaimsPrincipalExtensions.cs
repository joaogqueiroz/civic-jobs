using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManager.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the owner id from the "sub" claim. The default authorization policy already
    /// guarantees it is a Guid, so failure here indicates a misconfigured endpoint.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated principal has no valid 'sub' claim.");
}

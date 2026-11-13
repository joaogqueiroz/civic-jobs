using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserViewModel>> Register(RegisterInputModel input, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(input, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthTokenViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenViewModel>> Login(LoginInputModel input, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(input, cancellationToken);
        SessionCookie.Append(Response, token.AccessToken, token.ExpiresAt);

        return Ok(token);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        SessionCookie.Delete(Response);

        return NoContent();
    }
}

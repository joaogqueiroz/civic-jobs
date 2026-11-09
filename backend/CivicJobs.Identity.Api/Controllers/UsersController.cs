using CivicJobs.Application.Services;
using CivicJobs.Application.ViewModels;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserViewModel>> Me(CancellationToken cancellationToken) =>
        Ok(await userService.GetCurrentAsync(User.GetUserId(), cancellationToken));
}

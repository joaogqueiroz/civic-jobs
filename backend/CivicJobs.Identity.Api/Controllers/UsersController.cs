using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class UsersController(IUserService userService, IStaffService staffService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserViewModel>> Me(CancellationToken cancellationToken) =>
        Ok(await userService.GetCurrentAsync(User.GetUserId(), cancellationToken));

    #region Staff of the admin's agency (FR-13, FR-14)

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<IReadOnlyList<StaffViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<StaffViewModel>>> ListStaff(CancellationToken cancellationToken) =>
        Ok(await staffService.ListAsync(User.GetUserId(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<StaffViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffViewModel>> CreateStaff(CreateStaffInputModel input, CancellationToken cancellationToken)
    {
        var staff = await staffService.CreateAsync(User.GetUserId(), input, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, staff);
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<StaffViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffViewModel>> ChangeRole(
        Guid id,
        ChangeRoleInputModel input,
        CancellationToken cancellationToken) =>
        Ok(await staffService.ChangeRoleAsync(User.GetUserId(), id, input, cancellationToken));

    #endregion
}

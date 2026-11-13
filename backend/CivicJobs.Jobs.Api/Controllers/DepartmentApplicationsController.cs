using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Jobs.Api.Controllers;

/// <summary>The hiring manager's decisions on applications to their department's postings (FR-20, FR-21).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.HiringManager))]
[Route("api/department")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DepartmentApplicationsController(IDepartmentApplicationService applicationService) : ControllerBase
{
    [HttpGet("job-postings/{id:guid}/applications")]
    [ProducesResponseType<IReadOnlyList<PostingApplicationViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PostingApplicationViewModel>>> ListByPosting(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await applicationService.ListByPostingAsync(User.GetUserId(), id, cancellationToken));

    [HttpPut("applications/{id:guid}/status")]
    [ProducesResponseType<PostingApplicationViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PostingApplicationViewModel>> ChangeStatus(
        Guid id,
        ChangeApplicationStatusInputModel input,
        CancellationToken cancellationToken) =>
        Ok(await applicationService.ChangeStatusAsync(User.GetUserId(), id, input, cancellationToken));
}

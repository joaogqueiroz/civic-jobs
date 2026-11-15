using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Jobs.Api.Controllers;

/// <summary>The candidate's applications (FR-18, FR-19).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Candidate))]
[Route("api/applications")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ApplicationsController(IJobApplicationService applicationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CandidateApplicationViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CandidateApplicationViewModel>>> ListMine(CancellationToken cancellationToken) =>
        Ok(await applicationService.ListMineAsync(User.GetUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<CandidateApplicationViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CandidateApplicationViewModel>> Apply(ApplyInputModel input, CancellationToken cancellationToken)
    {
        var application = await applicationService.ApplyAsync(User.GetUserId(), input, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, application);
    }
}

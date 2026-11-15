using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Jobs.Api.Controllers;

/// <summary>Postings of the hiring manager's department: list, approve and reject (FR-15 to FR-17).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.HiringManager))]
[Route("api/department/job-postings")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DepartmentJobPostingsController(IDepartmentJobPostingService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<JobPostingViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPostingViewModel>>> List(
        [FromQuery] JobPostingStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(User.GetUserId(), status, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingViewModel>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingViewModel>> Approve(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.ApproveAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingViewModel>> Reject(
        Guid id,
        RejectJobPostingInputModel input,
        CancellationToken cancellationToken) =>
        Ok(await service.RejectAsync(User.GetUserId(), id, input, cancellationToken));
}

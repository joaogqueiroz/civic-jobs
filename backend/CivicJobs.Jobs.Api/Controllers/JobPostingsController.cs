using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Jobs.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/job-postings")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class JobPostingsController(IJobPostingService jobPostingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<JobPostingViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPostingViewModel>>> List(
        [FromQuery] JobPostingStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await jobPostingService.ListAsync(User.GetUserId(), status, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingViewModel>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await jobPostingService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobPostingViewModel>> Create(
        CreateJobPostingInputModel input,
        CancellationToken cancellationToken)
    {
        var posting = await jobPostingService.CreateAsync(User.GetUserId(), input, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = posting.Id }, posting);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingViewModel>> Update(
        Guid id,
        UpdateJobPostingInputModel input,
        CancellationToken cancellationToken) =>
        Ok(await jobPostingService.UpdateAsync(User.GetUserId(), id, input, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await jobPostingService.DeleteAsync(User.GetUserId(), id, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingViewModel>> Publish(Guid id, CancellationToken cancellationToken) =>
        Ok(await jobPostingService.PublishAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType<JobPostingViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingViewModel>> Close(Guid id, CancellationToken cancellationToken) =>
        Ok(await jobPostingService.CloseAsync(User.GetUserId(), id, cancellationToken));
}

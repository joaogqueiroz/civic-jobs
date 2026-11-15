using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Jobs.Api.Controllers;

/// <summary>Anonymous list of open positions for candidates (FR-11).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/job-postings")]
public sealed class PublicJobPostingsController(IJobPostingService jobPostingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PublicJobPostingViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicJobPostingViewModel>>> List(CancellationToken cancellationToken) =>
        Ok(await jobPostingService.ListPublicAsync(cancellationToken));
}

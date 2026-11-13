using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using CivicJobs.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicJobs.Identity.Api.Controllers;

/// <summary>Departments of the admin's agency (FR-12).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/departments")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DepartmentViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentViewModel>>> List(CancellationToken cancellationToken) =>
        Ok(await departmentService.ListAsync(User.GetUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<DepartmentViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentViewModel>> Create(
        CreateDepartmentInputModel input,
        CancellationToken cancellationToken)
    {
        var department = await departmentService.CreateAsync(User.GetUserId(), input, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, department);
    }
}

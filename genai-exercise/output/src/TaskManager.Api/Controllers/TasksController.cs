using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Auth;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Core.Enums;

namespace TaskManager.Api.Controllers;

/// <summary>CRUD for the current user's tasks. Thin by design: rules live in Core/Application.</summary>
[ApiController]
[Authorize]
[Route("api/tasks")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    private const string GetByIdRoute = "GetTaskById";

    /// <summary>Lists the current user's tasks, optionally filtered by status.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List(
        [FromQuery] TaskItemStatus? status, CancellationToken cancellationToken) =>
        Ok(await taskService.ListAsync(User.GetUserId(), status, cancellationToken));

    /// <summary>Gets one of the current user's tasks.</summary>
    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await taskService.GetAsync(User.GetUserId(), id, cancellationToken));

    /// <summary>Creates a task owned by the current user.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(
        CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await taskService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtRoute(GetByIdRoute, new { id = created.Id }, created);
    }

    /// <summary>Replaces a task. A task that is Done cannot move to another status.</summary>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        Ok(await taskService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    /// <summary>Deletes a task.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}

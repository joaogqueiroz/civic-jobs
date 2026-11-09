using TaskManager.Application.Common;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Core.Entities;
using TaskManager.Core.Enums;
using TaskManager.Core.Exceptions;
using TaskManager.Core.Interfaces;

namespace TaskManager.Application.Tasks;

public sealed class TaskService(ITaskRepository repository, TimeProvider timeProvider) : ITaskService
{
    private const string Resource = "Task";

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken)
    {
        if (status is { } s && !Enum.IsDefined(s))
        {
            throw new DomainValidationException("status", $"'{(int)s}' is not a valid status.");
        }

        var tasks = await repository.ListByOwnerAsync(userId, status, cancellationToken);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedOrThrowAsync(userId, taskId, cancellationToken);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateAsync(
        Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = TaskItem.Create(userId, request.Title, request.Description, request.DueDate, timeProvider);

        await repository.AddAsync(task, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await GetOwnedOrThrowAsync(userId, taskId, cancellationToken);
        task.Update(request.Title, request.Description, request.Status, request.DueDate, timeProvider);

        await repository.SaveChangesAsync(cancellationToken);

        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedOrThrowAsync(userId, taskId, cancellationToken);

        repository.Remove(task);
        await repository.SaveChangesAsync(cancellationToken);
    }

    // Tasks owned by someone else surface as "not found" so their existence is not leaked.
    private async Task<TaskItem> GetOwnedOrThrowAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        await repository.GetByIdForOwnerAsync(taskId, userId, cancellationToken)
        ?? throw new NotFoundException(Resource, taskId);
}

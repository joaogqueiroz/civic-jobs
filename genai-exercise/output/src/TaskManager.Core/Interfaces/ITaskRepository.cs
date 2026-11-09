using TaskManager.Core.Entities;
using TaskManager.Core.Enums;

namespace TaskManager.Core.Interfaces;

/// <summary>
/// Persistence abstraction for tasks. Every read is scoped to an owner so that
/// another user's task is indistinguishable from a task that does not exist.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid userId, TaskItemStatus? status, CancellationToken cancellationToken);

    Task<TaskItem?> GetByIdForOwnerAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

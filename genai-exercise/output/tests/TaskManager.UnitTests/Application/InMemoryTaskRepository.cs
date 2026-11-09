using TaskManager.Core.Entities;
using TaskManager.Core.Enums;
using TaskManager.Core.Interfaces;

namespace TaskManager.UnitTests.Application;

/// <summary>Hand-rolled fake: simpler and more honest than mocking a repository.</summary>
internal sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Items { get; } = [];
    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid userId, TaskItemStatus? status, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(Items
            .Where(t => t.UserId == userId && (status is null || t.Status == status))
            .OrderBy(t => t.DueDate)
            .ToList());

    public Task<TaskItem?> GetByIdForOwnerAsync(Guid id, Guid userId, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id && t.UserId == userId));

    public Task AddAsync(TaskItem task, CancellationToken ct)
    {
        Items.Add(task);
        return Task.CompletedTask;
    }

    public void Remove(TaskItem task) => Items.Remove(task);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

using Microsoft.EntityFrameworkCore;
using TaskManager.Core.Entities;
using TaskManager.Core.Enums;
using TaskManager.Core.Interfaces;

namespace TaskManager.Infrastructure.Persistence;

internal sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Tasks.AsNoTracking().Where(t => t.UserId == userId);

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        return await query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<TaskItem?> GetByIdForOwnerAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken) =>
        await db.Tasks.AddAsync(task, cancellationToken);

    public void Remove(TaskItem task) => db.Tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

using Microsoft.Extensions.Time.Testing;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Core.Enums;
using TaskManager.Core.Exceptions;

namespace TaskManager.UnitTests.Application;

public sealed class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2030, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly InMemoryTaskRepository _repository = new();
    private readonly TaskService _sut;

    public TaskServiceTests() => _sut = new TaskService(_repository, new FakeTimeProvider(Now));

    private static CreateTaskRequest Create(string title = "Some task", int dueInDays = 1) =>
        new() { Title = title, DueDate = Today.AddDays(dueInDays) };

    private static UpdateTaskRequest Update(TaskItemStatus status, string title = "Some task", int dueInDays = 1) =>
        new() { Title = title, Status = status, DueDate = Today.AddDays(dueInDays) };

    [Fact]
    public async Task Create_persists_task_owned_by_caller()
    {
        var response = await _sut.CreateAsync(_alice, Create("Buy milk"), CancellationToken.None);

        var stored = Assert.Single(_repository.Items);
        Assert.Equal(_alice, stored.UserId);
        Assert.Equal(stored.Id, response.Id);
        Assert.Equal("Buy milk", response.Title);
        Assert.Equal(TaskItemStatus.Todo, response.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Create_with_invalid_data_does_not_persist()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _sut.CreateAsync(_alice, Create(title: " "), CancellationToken.None));

        Assert.Empty(_repository.Items);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task List_returns_only_callers_tasks_filtered_by_status()
    {
        var a1 = await _sut.CreateAsync(_alice, Create("Alice one"), CancellationToken.None);
        var a2 = await _sut.CreateAsync(_alice, Create("Alice two"), CancellationToken.None);
        await _sut.CreateAsync(_bob, Create("Bob one"), CancellationToken.None);
        await _sut.UpdateAsync(_alice, a2.Id, Update(TaskItemStatus.Doing, "Alice two"), CancellationToken.None);

        var all = await _sut.ListAsync(_alice, null, CancellationToken.None);
        var doing = await _sut.ListAsync(_alice, TaskItemStatus.Doing, CancellationToken.None);

        Assert.Equivalent(new[] { a1.Id, a2.Id }, all.Select(t => t.Id).ToArray());
        Assert.Equal(a2.Id, Assert.Single(doing).Id);
    }

    [Fact]
    public async Task List_rejects_undefined_status()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _sut.ListAsync(_alice, (TaskItemStatus)99, CancellationToken.None));
    }

    [Fact]
    public async Task Get_task_of_another_user_throws_not_found()
    {
        var bobs = await _sut.CreateAsync(_bob, Create(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(_alice, bobs.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Update_applies_changes()
    {
        var created = await _sut.CreateAsync(_alice, Create(), CancellationToken.None);

        var updated = await _sut.UpdateAsync(
            _alice, created.Id, Update(TaskItemStatus.Done, "Finished", dueInDays: 7), CancellationToken.None);

        Assert.Equal("Finished", updated.Title);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal(Today.AddDays(7), updated.DueDate);
    }

    [Fact]
    public async Task Update_done_task_to_other_status_is_rejected()
    {
        var created = await _sut.CreateAsync(_alice, Create(), CancellationToken.None);
        await _sut.UpdateAsync(_alice, created.Id, Update(TaskItemStatus.Done), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _sut.UpdateAsync(_alice, created.Id, Update(TaskItemStatus.Todo), CancellationToken.None));

        Assert.Contains("Status", ex.Errors.Keys);
    }

    [Fact]
    public async Task Update_task_of_another_user_throws_not_found_and_changes_nothing()
    {
        var bobs = await _sut.CreateAsync(_bob, Create("Bob's task"), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(_alice, bobs.Id, Update(TaskItemStatus.Done, "Hijacked"), CancellationToken.None));

        Assert.Equal("Bob's task", Assert.Single(_repository.Items).Title);
    }

    [Fact]
    public async Task Delete_removes_task()
    {
        var created = await _sut.CreateAsync(_alice, Create(), CancellationToken.None);

        await _sut.DeleteAsync(_alice, created.Id, CancellationToken.None);

        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task Delete_task_of_another_user_throws_not_found_and_keeps_it()
    {
        var bobs = await _sut.CreateAsync(_bob, Create(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(_alice, bobs.Id, CancellationToken.None));

        Assert.Single(_repository.Items);
    }
}

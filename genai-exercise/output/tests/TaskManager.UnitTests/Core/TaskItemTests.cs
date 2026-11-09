using Microsoft.Extensions.Time.Testing;
using TaskManager.Core.Entities;
using TaskManager.Core.Enums;
using TaskManager.Core.Exceptions;

namespace TaskManager.UnitTests.Core;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2030, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly FakeTimeProvider _clock = new(Now);

    private TaskItem NewTask(string title = "Valid title", DateOnly? due = null) =>
        TaskItem.Create(Owner, title, "desc", due ?? Today.AddDays(1), _clock);

    [Fact]
    public void Create_with_valid_data_sets_defaults()
    {
        var task = TaskItem.Create(Owner, "  Write report  ", "  details ", Today.AddDays(3), _clock);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(Owner, task.UserId);
        Assert.Equal("Write report", task.Title);
        Assert.Equal("details", task.Description);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal(Today.AddDays(3), task.DueDate);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_rejects_missing_or_whitespace_title(string? title)
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => TaskItem.Create(Owner, title, null, Today.AddDays(1), _clock));

        Assert.Contains(nameof(TaskItem.Title), ex.Errors.Keys);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(151)]
    public void Create_rejects_title_outside_length_bounds(int length)
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewTask(new string('a', length)));
        Assert.Contains(nameof(TaskItem.Title), ex.Errors.Keys);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(150)]
    public void Create_accepts_title_at_length_bounds(int length)
    {
        var task = NewTask(new string('a', length));
        Assert.Equal(length, task.Title.Length);
    }

    [Fact]
    public void Create_measures_title_length_after_trimming()
    {
        Assert.Throws<DomainValidationException>(() => NewTask("  ab  "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-365)]
    public void Create_rejects_due_date_today_or_in_the_past(int offsetDays)
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewTask(due: Today.AddDays(offsetDays)));
        Assert.Contains(nameof(TaskItem.DueDate), ex.Errors.Keys);
    }

    [Fact]
    public void Create_rejects_empty_owner()
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => TaskItem.Create(Guid.Empty, "Valid title", null, Today.AddDays(1), _clock));
        Assert.Contains(nameof(TaskItem.UserId), ex.Errors.Keys);
    }

    [Fact]
    public void Create_reports_all_errors_at_once()
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => TaskItem.Create(Owner, "", null, Today, _clock));

        Assert.Contains(nameof(TaskItem.Title), ex.Errors.Keys);
        Assert.Contains(nameof(TaskItem.DueDate), ex.Errors.Keys);
    }

    [Fact]
    public void Update_changes_fields_and_UpdatedAt()
    {
        var task = NewTask();
        _clock.Advance(TimeSpan.FromHours(2));

        task.Update("New title", null, TaskItemStatus.Doing, Today.AddDays(5), _clock);

        Assert.Equal("New title", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskItemStatus.Doing, task.Status);
        Assert.Equal(Today.AddDays(5), task.DueDate);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now.AddHours(2), task.UpdatedAt);
    }

    [Theory]
    [InlineData(TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Doing)]
    public void Update_rejects_moving_a_done_task_to_another_status(TaskItemStatus newStatus)
    {
        var task = NewTask();
        task.Update(task.Title, null, TaskItemStatus.Done, task.DueDate, _clock);

        var ex = Assert.Throws<DomainValidationException>(
            () => task.Update(task.Title, null, newStatus, task.DueDate, _clock));

        Assert.Contains(nameof(TaskItem.Status), ex.Errors.Keys);
        Assert.Equal(TaskItemStatus.Done, task.Status);
    }

    [Fact]
    public void Update_allows_editing_a_done_task_that_stays_done()
    {
        var task = NewTask();
        task.Update(task.Title, null, TaskItemStatus.Done, task.DueDate, _clock);

        task.Update("Renamed", "notes", TaskItemStatus.Done, task.DueDate, _clock);

        Assert.Equal("Renamed", task.Title);
        Assert.Equal(TaskItemStatus.Done, task.Status);
    }

    [Fact]
    public void Update_with_unchanged_past_due_date_is_allowed()
    {
        var task = NewTask(due: Today.AddDays(1));
        _clock.Advance(TimeSpan.FromDays(10)); // due date is now in the past

        task.Update("Still valid", null, TaskItemStatus.Doing, task.DueDate, _clock);

        Assert.Equal(TaskItemStatus.Doing, task.Status);
    }

    [Fact]
    public void Update_rejects_changing_due_date_to_today_or_past()
    {
        var task = NewTask(due: Today.AddDays(5));

        var ex = Assert.Throws<DomainValidationException>(
            () => task.Update(task.Title, null, TaskItemStatus.Todo, Today, _clock));

        Assert.Contains(nameof(TaskItem.DueDate), ex.Errors.Keys);
        Assert.Equal(Today.AddDays(5), task.DueDate);
    }

    [Fact]
    public void Update_rejects_undefined_status_value()
    {
        var task = NewTask();

        var ex = Assert.Throws<DomainValidationException>(
            () => task.Update(task.Title, null, (TaskItemStatus)42, task.DueDate, _clock));

        Assert.Contains(nameof(TaskItem.Status), ex.Errors.Keys);
    }

    [Fact]
    public void Failed_update_leaves_task_unchanged()
    {
        var task = NewTask("Original");

        Assert.Throws<DomainValidationException>(
            () => task.Update("x", null, TaskItemStatus.Doing, Today.AddDays(9), _clock));

        Assert.Equal("Original", task.Title);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal(Now, task.UpdatedAt);
    }
}

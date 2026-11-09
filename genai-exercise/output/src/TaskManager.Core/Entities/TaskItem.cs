using TaskManager.Core.Enums;
using TaskManager.Core.Exceptions;

namespace TaskManager.Core.Entities;

/// <summary>
/// A task owned by a user. Named TaskItem to avoid clashing with System.Threading.Tasks.Task.
/// All invariants are enforced here; the entity can never be in an invalid state.
/// </summary>
public sealed class TaskItem
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 150;

    // Required by EF Core.
    private TaskItem()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(
        Guid userId,
        string? title,
        string? description,
        DateOnly dueDate,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow();
        var errors = new Dictionary<string, List<string>>();

        if (userId == Guid.Empty)
        {
            AddError(errors, nameof(UserId), "A task must have an owner.");
        }

        ValidateTitle(title, errors);
        ValidateDueDate(dueDate, Today(now), errors);
        ThrowIfAny(errors);

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title!.Trim(),
            Description = NormalizeDescription(description),
            Status = TaskItemStatus.Todo,
            DueDate = dueDate,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string? title,
        string? description,
        TaskItemStatus status,
        DateOnly dueDate,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow();
        var errors = new Dictionary<string, List<string>>();

        ValidateTitle(title, errors);
        ValidateStatusTransition(status, errors);

        // The due date rule only applies when the due date actually changes, so an
        // overdue task can still be edited without being forced to move its date.
        if (dueDate != DueDate)
        {
            ValidateDueDate(dueDate, Today(now), errors);
        }

        ThrowIfAny(errors);

        Title = title!.Trim();
        Description = NormalizeDescription(description);
        Status = status;
        DueDate = dueDate;
        UpdatedAt = now;
    }

    private void ValidateStatusTransition(TaskItemStatus newStatus, Dictionary<string, List<string>> errors)
    {
        if (!Enum.IsDefined(newStatus))
        {
            AddError(errors, nameof(Status), $"'{(int)newStatus}' is not a valid status.");
            return;
        }

        if (Status == TaskItemStatus.Done && newStatus != TaskItemStatus.Done)
        {
            AddError(errors, nameof(Status), "A task that is Done cannot be moved to another status.");
        }
    }

    private static void ValidateTitle(string? title, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            AddError(errors, nameof(Title), "Title is required and cannot be empty or whitespace.");
            return;
        }

        var length = title.Trim().Length;
        if (length is < TitleMinLength or > TitleMaxLength)
        {
            AddError(errors, nameof(Title),
                $"Title must be between {TitleMinLength} and {TitleMaxLength} characters.");
        }
    }

    private static void ValidateDueDate(DateOnly dueDate, DateOnly today, Dictionary<string, List<string>> errors)
    {
        if (dueDate <= today)
        {
            AddError(errors, nameof(DueDate), $"Due date must be after today ({today:yyyy-MM-dd}).");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static DateOnly Today(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            errors[field] = list = [];
        }

        list.Add(message);
    }

    private static void ThrowIfAny(Dictionary<string, List<string>> errors)
    {
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}

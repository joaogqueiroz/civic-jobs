using TaskManager.Core.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Full replacement payload for PUT. All required members must be supplied.</summary>
public sealed class UpdateTaskRequest
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required TaskItemStatus Status { get; init; }

    /// <summary>If changed, must be after today (UTC).</summary>
    public required DateOnly DueDate { get; init; }
}

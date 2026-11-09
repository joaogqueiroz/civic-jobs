namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Payload for creating a task. The owner is taken from the token, never from the body.</summary>
public sealed class CreateTaskRequest
{
    /// <example>Write the quarterly report</example>
    public required string Title { get; init; }

    /// <example>Include the sales figures from Q3.</example>
    public string? Description { get; init; }

    /// <summary>Must be after today (UTC).</summary>
    public required DateOnly DueDate { get; init; }
}

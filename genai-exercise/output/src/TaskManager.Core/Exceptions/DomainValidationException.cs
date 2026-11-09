namespace TaskManager.Core.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated. Carries field-level errors so the
/// API can translate them into a ValidationProblemDetails response.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more domain validation errors occurred.")
    {
        Errors = errors;
    }

    public DomainValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

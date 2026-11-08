namespace CivicJobs.Core.Exceptions;

public sealed class DomainValidationException(string message) : DomainException(message);

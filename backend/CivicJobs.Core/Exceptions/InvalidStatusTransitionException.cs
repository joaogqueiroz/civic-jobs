namespace CivicJobs.Core.Exceptions;

public sealed class InvalidStatusTransitionException(string message) : DomainException(message);

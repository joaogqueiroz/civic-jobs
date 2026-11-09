using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

internal static class Guard
{
    public static void Required(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }
    }
}

using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string email, string fullName, string passwordHash, DateTimeOffset createdAt)
    {
        EnsureRequired(email, nameof(Email));
        EnsureRequired(fullName, nameof(FullName));
        EnsureRequired(passwordHash, nameof(PasswordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = NormalizeEmail(email),
            FullName = fullName,
            PasswordHash = passwordHash,
            CreatedAt = createdAt
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static void EnsureRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }
    }
}

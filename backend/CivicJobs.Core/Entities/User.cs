namespace CivicJobs.Core.Entities;

public class User
{
    public const int FullNameMaxLength = 100;

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
        Guard.Required(email, nameof(Email));
        Guard.Required(fullName, nameof(FullName));
        Guard.Required(passwordHash, nameof(PasswordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = createdAt
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

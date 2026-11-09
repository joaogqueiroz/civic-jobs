namespace CivicJobs.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Returns true when <paramref name="password"/> matches <paramref name="passwordHash"/>.
    /// When <paramref name="passwordHash"/> is null (unknown user), implementations must still do the same
    /// amount of work and return false, so login takes the same time whether or not the email exists.
    /// </summary>
    bool Verify(string? passwordHash, string password);
}

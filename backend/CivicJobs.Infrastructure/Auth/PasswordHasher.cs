using CivicJobs.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace CivicJobs.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    // ASP.NET Core's PasswordHasher<T> (PBKDF2) doesn't use the user instance, so a shared placeholder is passed.
    private static readonly object HashOwner = new();
    private static readonly PasswordHasher<object> Hasher = new();

    // Verified against when there's no stored hash, so an unknown email costs the same PBKDF2 work as a known one.
    private static readonly Lazy<string> DummyHash = new(() => Hasher.HashPassword(HashOwner, Guid.NewGuid().ToString()));

    public string Hash(string password) => Hasher.HashPassword(HashOwner, password);

    public bool Verify(string? passwordHash, string password)
    {
        var result = Hasher.VerifyHashedPassword(HashOwner, passwordHash ?? DummyHash.Value, password);

        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}

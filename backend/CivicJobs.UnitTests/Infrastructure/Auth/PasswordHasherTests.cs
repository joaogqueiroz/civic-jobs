using CivicJobs.Infrastructure.Auth;
using Shouldly;

namespace CivicJobs.UnitTests.Infrastructure.Auth;

public class PasswordHasherTests
{
    private const string Password = "Demo@12345";

    private readonly PasswordHasher _hasher = new();

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void Hash_ReturnsValueDifferentFromPassword()
    {
        var hash = _hasher.Hash(Password);

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotContain(Password);
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash(Password);

        _hasher.Verify(hash, Password).ShouldBeTrue();
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash(Password);

        _hasher.Verify(hash, "Wrong@12345").ShouldBeFalse();
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public void Verify_WithoutStoredHash_ReturnsFalse()
    {
        _hasher.Verify(null, Password).ShouldBeFalse();
    }
}

namespace TaskManager.Core.Entities;

/// <summary>
/// Basic user model. Users are identified by the JWT "sub" claim; there is no
/// registration/login flow in this API.
/// </summary>
public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
}

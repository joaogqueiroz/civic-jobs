namespace CivicJobs.Application.ViewModels;

public sealed record AuthTokenViewModel(string AccessToken, DateTimeOffset ExpiresAt);

using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class AuthService(
    IValidator<RegisterInputModel> registerValidator,
    IValidator<LoginInputModel> loginValidator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<UserViewModel> RegisterAsync(RegisterInputModel input, CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(input, cancellationToken);

        var email = User.NormalizeEmail(input.Email);

        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("An account with this email already exists. Log in, or use a different email.");
        }

        var user = User.CreateCandidate(
            email,
            input.FullName,
            passwordHasher.Hash(input.Password),
            timeProvider.GetUtcNow());

        await users.AddAsync(user, cancellationToken);

        return UserViewModel.FromEntity(user);
    }

    public async Task<AuthTokenViewModel> LoginAsync(LoginInputModel input, CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(input, cancellationToken);

        var user = await users.GetByEmailAsync(User.NormalizeEmail(input.Email), cancellationToken);

        // Always verify, even for an unknown email, and use the same error for both cases,
        // so neither the response nor its timing reveals which emails are registered.
        var passwordMatches = passwordHasher.Verify(user?.PasswordHash, input.Password);

        if (user is null || !passwordMatches)
        {
            throw new InvalidCredentialsException();
        }

        return tokenService.CreateToken(user);
    }
}

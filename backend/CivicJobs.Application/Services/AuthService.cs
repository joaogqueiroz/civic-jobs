using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services;

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
            throw new ConflictException("Email is already in use.");
        }

        var user = User.Create(
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

        // Same error for unknown email and wrong password, so the API doesn't reveal which emails are registered.
        if (user is null || !passwordHasher.Verify(user.PasswordHash, input.Password))
        {
            throw new InvalidCredentialsException();
        }

        return tokenService.CreateToken(user);
    }
}

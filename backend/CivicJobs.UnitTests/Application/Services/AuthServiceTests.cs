using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services;
using CivicJobs.Application.Validators;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class AuthServiceTests
{
    private const string Email = "recruiter@cityofexample.gov";
    private const string Password = "Demo@12345";
    private const string PasswordHash = "hashed-password";

    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    public AuthServiceTests()
    {
        _passwordHasher.Hash(Password).Returns(PasswordHash);
        _passwordHasher.Verify(PasswordHash, Password).Returns(true);
    }

    #region Register

    [Fact]
    [Trait("Requirement", "FR-01")]
    [Trait("Rule", "BR-01")]
    public async Task RegisterAsync_WithValidInput_ReturnsUserWithLowercaseEmail()
    {
        var input = RegisterInput(email: "Recruiter@CityOfExample.GOV");

        var result = await CreateService().RegisterAsync(input, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldNotBe(Guid.Empty),
            () => result.Email.ShouldBe(Email),
            () => result.FullName.ShouldBe("Jane Doe"),
            () => result.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Requirement", "FR-01")]
    public async Task RegisterAsync_WithValidInput_SavesUserWithHashedPassword()
    {
        User? saved = null;
        _users
            .When(r => r.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<User>());

        var result = await CreateService().RegisterAsync(RegisterInput(), CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.ShouldSatisfyAllConditions(
            () => saved.Id.ShouldBe(result.Id),
            () => saved.PasswordHash.ShouldBe(PasswordHash));
    }

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void UserViewModel_DoesNotExposePasswordHash()
    {
        typeof(UserViewModel).GetProperty(nameof(User.PasswordHash)).ShouldBeNull();
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task RegisterAsync_WhenEmailIsAlreadyInUse_ThrowsConflictException()
    {
        StoredUser();
        var input = RegisterInput(email: "RECRUITER@cityofexample.gov");

        var register = () => CreateService().RegisterAsync(input, CancellationToken.None);

        (await register.ShouldThrowAsync<ConflictException>())
            .Message.ShouldBe("An account with this email already exists. Log in, or use a different email.");
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    [Trait("Rule", "BR-02")]
    public async Task RegisterAsync_WithWeakPassword_ThrowsValidationException()
    {
        var input = RegisterInput(password: "password");

        var register = () => CreateService().RegisterAsync(input, CancellationToken.None);

        var exception = await register.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterInputModel.Password));
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    #endregion

    #region Login

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        var user = StoredUser();
        var token = new AuthTokenViewModel("jwt-token", Now.AddHours(1));
        _tokenService.CreateToken(user).Returns(token);

        var result = await CreateService().LoginAsync(LoginInput(), CancellationToken.None);

        result.ShouldBe(token);
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public async Task LoginAsync_WithMixedCaseEmail_ReturnsAccessToken()
    {
        var user = StoredUser();
        var token = new AuthTokenViewModel("jwt-token", Now.AddHours(1));
        _tokenService.CreateToken(user).Returns(token);

        var result = await CreateService().LoginAsync(LoginInput(email: " Recruiter@CityOfExample.GOV "), CancellationToken.None);

        result.ShouldBe(token);
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task LoginAsync_WhenEmailIsUnknown_ThrowsInvalidCredentialsException()
    {
        var login = () => CreateService().LoginAsync(LoginInput(), CancellationToken.None);

        await login.ShouldThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task LoginAsync_WhenEmailIsUnknown_StillVerifiesThePassword()
    {
        var login = () => CreateService().LoginAsync(LoginInput(), CancellationToken.None);

        await login.ShouldThrowAsync<InvalidCredentialsException>();

        // Response time is the only thing this protects, and it can't be measured reliably in a unit test.
        // Verifying the password even without a stored hash keeps unknown and known emails equally slow,
        // so an attacker can't find registered emails by timing the login endpoint.
        _passwordHasher.Received(1).Verify(null, Password);
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task LoginAsync_WhenPasswordIsWrong_ThrowsInvalidCredentialsException()
    {
        StoredUser();

        var login = () => CreateService().LoginAsync(LoginInput(password: "Wrong@12345"), CancellationToken.None);

        await login.ShouldThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    [Trait("Requirement", "FR-02")]
    public async Task LoginAsync_WithMissingPassword_ThrowsValidationException()
    {
        var login = () => CreateService().LoginAsync(LoginInput(password: ""), CancellationToken.None);

        await login.ShouldThrowAsync<ValidationException>();
    }

    #endregion

    private AuthService CreateService() =>
        new(
            new RegisterInputModelValidator(),
            new LoginInputModelValidator(),
            _users,
            _passwordHasher,
            _tokenService,
            _timeProvider);

    private User StoredUser()
    {
        var user = User.Create(Email, "Jane Doe", PasswordHash, Now);
        _users.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private static RegisterInputModel RegisterInput(string email = Email, string password = Password) => new()
    {
        FullName = "Jane Doe",
        Email = email,
        Password = password
    };

    private static LoginInputModel LoginInput(string email = Email, string password = Password) => new()
    {
        Email = email,
        Password = password
    };
}

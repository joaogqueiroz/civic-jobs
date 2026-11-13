using System.Text;
using CivicJobs.Application.Abstractions;
using CivicJobs.Core.Repositories;
using CivicJobs.Infrastructure.Auth;
using CivicJobs.Infrastructure.Persistence;
using CivicJobs.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CivicJobs.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CivicJobs";

    /// <summary>Registers persistence and the implementations of the Application abstractions (password hashing, tokens).</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing."),
            contentRootPath);

        services.AddDbContext<CivicJobsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IJobPostingRepository, JobPostingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAgencyRepository, AgencyRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => !string.IsNullOrWhiteSpace(jwt.Issuer)
                    && !string.IsNullOrWhiteSpace(jwt.Audience)
                    && Encoding.UTF8.GetByteCount(jwt.Key) >= 32
                    && jwt.ExpiryMinutes > 0,
                "Jwt settings need Issuer, Audience, a Key of at least 32 bytes and a positive ExpiryMinutes.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        await DbInitializer.InitializeAsync(
            provider.GetRequiredService<CivicJobsDbContext>(),
            provider.GetRequiredService<IPasswordHasher>(),
            provider.GetRequiredService<TimeProvider>(),
            cancellationToken);
    }

    // "Data Source=../civicjobs.db" is relative to each API's folder (its content root), not to the shell's
    // current directory, so both APIs share backend/civicjobs.db however they're started.
    private static string ResolveConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (builder.Mode != SqliteOpenMode.Memory
            && builder.DataSource != ":memory:"
            && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        }

        return builder.ToString();
    }
}

using System.Net.Http.Headers;
using CivicJobs.Application.Abstractions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using CivicJobs.Infrastructure.Persistence;
using CivicJobs.Jobs.Api.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CivicJobs.Jobs.ApiTests;

/// <summary>
/// Hosts the Jobs API in memory with its own SQLite in-memory database (kept alive by an open connection)
/// and a test-only JWT key. The "Testing" environment skips the development seed and Swagger.
/// </summary>
public sealed class JobsApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>Creates a recruiter in the database and returns a client that sends that recruiter's token.</summary>
    public async Task<HttpClient> CreateClientForNewRecruiterAsync()
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider;

        var user = User.Create(
            $"recruiter-{Guid.NewGuid():N}@cityofexample.gov",
            "Test Recruiter",
            "not-used-by-these-tests",
            provider.GetRequiredService<TimeProvider>().GetUtcNow());
        await provider.GetRequiredService<IUserRepository>().AddAsync(user, CancellationToken.None);
        var token = provider.GetRequiredService<ITokenService>().CreateToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "test-only-signing-key-that-is-long-enough-for-hs256");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CivicJobsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CivicJobsDbContext>>();
            services.AddDbContext<CivicJobsDbContext>(options => options.UseSqlite(_connection));

            // The worker would query the database before CreateHost applies the migrations; BR-11 is covered by unit tests.
            services.Remove(services.Single(d => d.ImplementationType == typeof(JobPostingExpirationWorker)));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CivicJobsDbContext>().Database.Migrate();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

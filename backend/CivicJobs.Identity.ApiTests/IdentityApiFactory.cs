using CivicJobs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CivicJobs.Identity.ApiTests;

/// <summary>
/// Hosts the Identity API in memory with its own SQLite in-memory database (kept alive by an open connection)
/// and a test-only JWT key. The "Testing" environment skips the development seed and Swagger.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

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

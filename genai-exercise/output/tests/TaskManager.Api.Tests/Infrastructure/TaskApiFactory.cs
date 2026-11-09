using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace TaskManager.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real API against an isolated SQLite file with test JWT settings and a fixed clock.
/// Runs in the "Testing" environment, so Development-only features are off.
/// </summary>
public class TaskApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Fixed "now" for domain rules. Token lifetimes still use the real clock.</summary>
    public static readonly DateTimeOffset Now = new(2030, 6, 15, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"taskmanager-tests-{Guid.NewGuid():N}.db");

    protected virtual string EnvironmentName => "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TaskDb"] = $"Data Source={_databasePath}",
            ["Jwt:Issuer"] = TestTokens.Issuer,
            ["Jwt:Audience"] = TestTokens.Audience,
            ["Jwt:SigningKey"] = TestTokens.SigningKey,
            ["Jwt:AccessTokenLifetimeMinutes"] = "5"
        }));

        builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(Now))));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

public sealed class DevelopmentTaskApiFactory : TaskApiFactory
{
    protected override string EnvironmentName => "Development";
}

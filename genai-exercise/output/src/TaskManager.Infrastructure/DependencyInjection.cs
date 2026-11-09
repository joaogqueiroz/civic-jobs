using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Core.Interfaces;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TaskDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolve the connection string lazily so hosts (and tests) can override configuration.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured.");
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<ITaskRepository, TaskRepository>();
        return services;
    }

    /// <summary>
    /// Creates the SQLite schema if it doesn't exist. Fine for this exercise; a production
    /// system would use EF Core migrations instead.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}

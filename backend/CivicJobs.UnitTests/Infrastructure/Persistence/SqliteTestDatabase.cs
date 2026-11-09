using CivicJobs.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.UnitTests.Infrastructure.Persistence;

/// <summary>
/// A real SQLite database that lives in memory for the duration of one test.
/// The connection stays open so the database survives across several DbContext instances,
/// which lets a test write with one context and read back with a fresh one (no change-tracker cache).
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public CivicJobsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CivicJobsDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

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
    private readonly List<CivicJobsDbContext> _contexts = [];

    public SqliteTestDatabase()
    {
        _connection.Open();
        CreateContext().Database.EnsureCreated();
    }

    public CivicJobsDbContext CreateContext()
    {
        var context = new CivicJobsDbContext(
            new DbContextOptionsBuilder<CivicJobsDbContext>().UseSqlite(_connection).Options);
        _contexts.Add(context);
        return context;
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            context.Dispose();
        }

        _connection.Dispose();
    }
}

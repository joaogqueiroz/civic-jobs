using CivicJobs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CivicJobs.Infrastructure.Persistence;

public sealed class CivicJobsDbContext(DbContextOptions<CivicJobsDbContext> options) : DbContext(options)
{
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CivicJobsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite can't sort or compare DateTimeOffset values, so they are stored as a 64-bit number (ticks + offset,
        // 0.1 ms precision). That number sorts by instant only because every timestamp is UTC (TimeProvider.GetUtcNow()).
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}

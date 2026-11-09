using CivicJobs.Application.Abstractions;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivicJobs.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds the demo data from docs/user-story.md. Safe to run more than once:
/// seeding only happens when there are no users.
/// </summary>
public static class DbInitializer
{
    public const string DemoPassword = "Demo@12345";

    public static async Task InitializeAsync(
        CivicJobsDbContext context,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);

        // Both APIs seed the same SQLite file at startup. Microsoft.Data.Sqlite starts transactions with
        // BEGIN IMMEDIATE, which takes the write lock up front, so the second API waits here and then sees the users.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var passwordHash = passwordHasher.Hash(DemoPassword);

        var parks = User.Create("recruiter@cityofexample.gov", "Jordan Lee", passwordHash, now.AddDays(-30));
        var publicWorks = User.Create("recruiter2@cityofexample.gov", "Taylor Morgan", passwordHash, now.AddDays(-30));

        context.Users.AddRange(parks, publicWorks);
        context.JobPostings.AddRange(
            Posting(parks, "Park Ranger", "Parks & Recreation", "Patrol city parks, enforce park rules and help visitors.",
                "Central Park District", EmploymentType.FullTime, 48_000m, 58_000m, today.AddDays(45), now.AddDays(-2)),
            Posting(parks, "Seasonal Lifeguard", "Parks & Recreation", "Supervise swimmers at city pools during the summer season.",
                "Riverside Aquatic Center", EmploymentType.Temporary, 32_000m, 36_000m, today.AddDays(60), now.AddDays(-1)),
            Published(Posting(parks, "Recreation Program Coordinator", "Parks & Recreation",
                "Plan and run youth and senior programs at community centers.", "Eastside Community Center",
                EmploymentType.FullTime, 55_000m, 65_000m, today.AddDays(30), now.AddDays(-10)), now.AddDays(-9)),
            Published(Posting(parks, "Groundskeeper", "Parks & Recreation", "Maintain lawns, trails and playgrounds across city parks.",
                "North Parks Yard", EmploymentType.PartTime, 28_000m, 34_000m, today.AddDays(21), now.AddDays(-8)), now.AddDays(-7)),
            Closed(Published(Posting(parks, "Community Center Assistant", "Parks & Recreation",
                "Front desk support and room bookings at the community center.", "Westside Community Center",
                EmploymentType.PartTime, 26_000m, 30_000m, today.AddDays(14), now.AddDays(-20)), now.AddDays(-19)), now.AddDays(-3)),
            Published(Posting(publicWorks, "Civil Engineer I", "Public Works", "Design and review street and drainage improvement projects.",
                "City Hall Annex", EmploymentType.FullTime, 72_000m, 85_000m, today.AddDays(30), now.AddDays(-6)), now.AddDays(-5)),
            Posting(publicWorks, "Maintenance Worker", "Public Works", "Repair streets, sidewalks and storm drains.",
                "Public Works Yard", EmploymentType.FullTime, 40_000m, 47_000m, today.AddDays(40), now.AddDays(-4)));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static JobPosting Posting(
        User owner,
        string title,
        string department,
        string description,
        string location,
        EmploymentType employmentType,
        decimal salaryMin,
        decimal salaryMax,
        DateOnly closingDate,
        DateTimeOffset createdAt) =>
        JobPosting.Create(owner.Id, title, department, description, location, employmentType, salaryMin, salaryMax, closingDate, createdAt);

    private static JobPosting Published(JobPosting posting, DateTimeOffset publishedAt)
    {
        posting.Publish(publishedAt);
        return posting;
    }

    private static JobPosting Closed(JobPosting posting, DateTimeOffset closedAt)
    {
        posting.Close(closedAt);
        return posting;
    }
}

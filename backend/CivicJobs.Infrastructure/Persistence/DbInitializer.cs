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

        var agency = Agency.Create("City of Example", now.AddDays(-60));
        var parksDepartment = Department.Create(agency.Id, "Parks & Recreation", now.AddDays(-60));
        var worksDepartment = Department.Create(agency.Id, "Public Works", now.AddDays(-60));

        User Staff(string email, string fullName, UserRole role, Department? department) =>
            User.CreateStaff(email, fullName, passwordHash, role, agency.Id, department?.Id, now.AddDays(-30));

        var admin = Staff("admin@cityofexample.gov", "Casey Rivera", UserRole.Admin, null);
        var parks = Staff("recruiter@cityofexample.gov", "Jordan Lee", UserRole.Recruiter, parksDepartment);
        var publicWorks = Staff("recruiter2@cityofexample.gov", "Taylor Morgan", UserRole.Recruiter, worksDepartment);
        var parksManager = Staff("manager@cityofexample.gov", "Sam Patel", UserRole.HiringManager, parksDepartment);
        var worksManager = Staff("manager2@cityofexample.gov", "Riley Chen", UserRole.HiringManager, worksDepartment);
        var candidate = User.CreateCandidate("candidate@example.com", "Alex Kim", passwordHash, now.AddDays(-15));

        JobPosting Posting(User owner, Department department, string title, string description, string location,
            EmploymentType employmentType, decimal salaryMin, decimal salaryMax, DateOnly closingDate, DateTimeOffset createdAt) =>
            JobPosting.Create(owner.Id, department.Id, department.Name, title, description, location, employmentType,
                salaryMin, salaryMax, closingDate, createdAt);

        context.Agencies.Add(agency);
        context.Departments.AddRange(parksDepartment, worksDepartment);
        context.Users.AddRange(admin, parks, publicWorks, parksManager, worksManager, candidate);
        context.JobPostings.AddRange(
            Posting(parks, parksDepartment, "Park Ranger", "Patrol city parks, enforce park rules and help visitors.",
                "Central Park District", EmploymentType.FullTime, 48_000m, 58_000m, today.AddDays(45), now.AddDays(-2)),
            Posting(parks, parksDepartment, "Seasonal Lifeguard", "Supervise swimmers at city pools during the summer season.",
                "Riverside Aquatic Center", EmploymentType.Temporary, 32_000m, 36_000m, today.AddDays(60), now.AddDays(-1)),
            Published(Posting(parks, parksDepartment, "Recreation Program Coordinator",
                "Plan and run youth and senior programs at community centers.", "Eastside Community Center",
                EmploymentType.FullTime, 55_000m, 65_000m, today.AddDays(30), now.AddDays(-10)), now.AddDays(-9)),
            Published(Posting(parks, parksDepartment, "Groundskeeper", "Maintain lawns, trails and playgrounds across city parks.",
                "North Parks Yard", EmploymentType.PartTime, 28_000m, 34_000m, today.AddDays(21), now.AddDays(-8)), now.AddDays(-7)),
            Closed(Published(Posting(parks, parksDepartment, "Community Center Assistant",
                "Front desk support and room bookings at the community center.", "Westside Community Center",
                EmploymentType.PartTime, 26_000m, 30_000m, today.AddDays(14), now.AddDays(-20)), now.AddDays(-19)), now.AddDays(-3)),
            Published(Posting(publicWorks, worksDepartment, "Civil Engineer I", "Design and review street and drainage improvement projects.",
                "City Hall Annex", EmploymentType.FullTime, 72_000m, 85_000m, today.AddDays(30), now.AddDays(-6)), now.AddDays(-5)),
            Posting(publicWorks, worksDepartment, "Maintenance Worker", "Repair streets, sidewalks and storm drains.",
                "Public Works Yard", EmploymentType.FullTime, 40_000m, 47_000m, today.AddDays(40), now.AddDays(-4)));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

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

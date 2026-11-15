using CivicJobs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicJobs.Infrastructure.Persistence.Configurations;

public sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("JobPostings");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Title).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Department).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(4000);
        builder.Property(p => p.Location).IsRequired().HasMaxLength(100);

        builder.Property(p => p.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.RejectionReason).HasMaxLength(JobPosting.RejectionReasonMaxLength);

        // SQLite stores decimals as TEXT and ignores precision. Queries that filter or sort by salary
        // can't be translated to SQL on SQLite; do that in memory or store salaries as integer cents.
        builder.Property(p => p.SalaryMin).HasPrecision(18, 2);
        builder.Property(p => p.SalaryMax).HasPrecision(18, 2);

        builder.HasIndex(p => new { p.CreatedByUserId, p.Status });
        builder.HasIndex(p => new { p.DepartmentId, p.Status });
    }
}

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

        builder.Property(p => p.Title).IsRequired().HasMaxLength(JobPosting.TitleMaxLength);
        builder.Property(p => p.Department).IsRequired().HasMaxLength(JobPosting.DepartmentMaxLength);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(JobPosting.DescriptionMaxLength);
        builder.Property(p => p.Location).IsRequired().HasMaxLength(JobPosting.LocationMaxLength);

        builder.Property(p => p.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.SalaryMin).HasPrecision(18, 2);
        builder.Property(p => p.SalaryMax).HasPrecision(18, 2);

        builder.HasIndex(p => new { p.CreatedByUserId, p.Status });
    }
}

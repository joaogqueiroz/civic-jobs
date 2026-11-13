using CivicJobs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicJobs.Infrastructure.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.CoverLetter).HasMaxLength(JobApplication.CoverLetterMaxLength);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        // One application per candidate and posting (BR-18), also used by the "my applications" list.
        builder.HasIndex(a => new { a.CandidateId, a.JobPostingId }).IsUnique();
        builder.HasIndex(a => a.JobPostingId);
    }
}

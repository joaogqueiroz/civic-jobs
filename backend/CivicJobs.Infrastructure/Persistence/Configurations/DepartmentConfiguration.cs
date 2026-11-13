using CivicJobs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicJobs.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        // NOCASE makes both the unique index and name comparisons ignore case (BR-14).
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
        builder.HasIndex(d => new { d.AgencyId, d.Name }).IsUnique();
    }
}

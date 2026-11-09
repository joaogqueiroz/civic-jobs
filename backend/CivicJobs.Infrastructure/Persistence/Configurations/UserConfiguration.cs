using CivicJobs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicJobs.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired();
    }
}

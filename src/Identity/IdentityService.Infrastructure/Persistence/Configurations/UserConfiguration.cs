using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();

        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property<List<string>>("_roles")
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Roles")
            .HasConversion(
                roles => string.Join(',', roles),
                value => value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property(u => u.RefreshTokenHash).HasMaxLength(512);
        builder.Property(u => u.RefreshTokenExpiresAtUtc);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

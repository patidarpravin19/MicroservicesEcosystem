using AccountingInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingInventory.Infrastructure.Persistence.Configurations;

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        // No schema specified deliberately: this table lives wherever PostgreSQL's
        // search_path currently points, i.e. inside the CURRENT tenant's schema (see
        // TenantSchemaConnectionInterceptor). Two different tenants can each have
        // their own physically separate "Vendors" table with overlapping usernames.
        builder.ToTable(Constants.DBConstants.DBTableNames.Vendors);
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Mobile).IsRequired().HasMaxLength(20);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Code).IsRequired();

        // Uniqueness only needs to hold WITHIN a schema — since schema-per-tenant
        // already guarantees physical separation between tenants, a plain unique
        // index (not a composite with TenantId) is correct and sufficient here.
        builder.HasIndex(u => u.Code).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Mobile).IsUnique();

        builder.Ignore(u => u.DomainEvents);
        builder.HasQueryFilter(u => !u.IsDeleted);

    }
}

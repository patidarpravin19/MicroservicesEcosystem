using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Sku).IsRequired().HasMaxLength(64);
        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.QuantityOnHand).IsRequired();
        builder.Property(s => s.WarehouseLocation).IsRequired().HasMaxLength(64);

        builder.HasIndex(s => s.Sku).IsUnique();

        builder.Ignore(s => s.DomainEvents);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

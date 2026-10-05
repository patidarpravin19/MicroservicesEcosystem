using AccountingInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingInventory.Infrastructure.Persistence.Configurations;

public sealed class SalesProductConfiguration : IEntityTypeConfiguration<SalesProduct>
{
    public void Configure(EntityTypeBuilder<SalesProduct> builder)
    {
        builder.ToTable(Constants.DBConstants.DBTableNames.SalesProducts);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SerialNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CustomerMobile).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CustomerAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SaleDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.ProductPrice).HasPrecision(18, 2);
        builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasPrecision(18, 2);
        builder.HasIndex(x => x.SerialNumber).IsUnique();
        builder.Ignore(x => x.DomainEvents);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

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
        builder.Property(x => x.Id).HasColumnOrder(0);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ProductId).HasColumnOrder(1);
        builder.Property(x => x.CustomerId).IsRequired().HasColumnOrder(2);
        builder.Property(x => x.SaleDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.SaleDate).HasColumnOrder(3);
        builder.Property(x => x.ProductPrice).HasPrecision(18, 2);
        builder.Property(x => x.ProductPrice).HasColumnOrder(4);
        builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
        builder.Property(x => x.SellingPrice).HasColumnOrder(5);
        builder.Property(x => x.Discount).HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasColumnOrder(6);
        builder.Property(x => x.CreatedAt).HasColumnOrder(7);
        builder.Property(x => x.CreatedBy).HasColumnOrder(8);
        builder.Property(x => x.ModifiedAt).HasColumnOrder(9);
        builder.Property(x => x.ModifiedBy).HasColumnOrder(10);
        builder.Property(x => x.IsActive).HasColumnOrder(11);
        builder.Property(x => x.IsDeleted).HasColumnOrder(12);
        builder.HasIndex(x => x.ProductId).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(x => x.CustomerId);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.DomainEvents);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

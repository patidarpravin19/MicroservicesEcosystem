using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingInventory.Infrastructure.Migrations.AccountingInventoryDb;

public sealed partial class AddProductSoldStatusAndBillNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bill_number", table: "products", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<bool>(
            name: "is_sold", table: "products", type: "boolean", nullable: false, defaultValue: false);

        // Older sales reduced quantity to zero. Restore the purchased unit count and
        // persist its sold state separately so sales no longer mutate purchase quantity.
        migrationBuilder.Sql("""
            UPDATE products AS product
            SET is_sold = TRUE,
                quantity = product.quantity + 1
            WHERE EXISTS (
                SELECT 1 FROM sales_products AS sale
                WHERE sale.is_deleted = FALSE
                  AND lower(sale.product_id) = product.id::text
            )
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE products AS product
            SET quantity = GREATEST(0, product.quantity - 1)
            WHERE EXISTS (
                SELECT 1 FROM sales_products AS sale
                WHERE sale.is_deleted = FALSE
                  AND lower(sale.product_id) = product.id::text
            )
            """);
        migrationBuilder.DropColumn(name: "bill_number", table: "products");
        migrationBuilder.DropColumn(name: "is_sold", table: "products");
    }
}

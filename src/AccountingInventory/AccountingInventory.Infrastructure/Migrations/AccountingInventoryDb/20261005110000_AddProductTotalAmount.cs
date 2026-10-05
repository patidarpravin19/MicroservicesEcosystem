using AccountingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingInventory.Infrastructure.Migrations.AccountingInventoryDb;

[DbContext(typeof(AccountingInventoryDbContext))]
[Migration("20261005110000_AddProductTotalAmount")]
public sealed class AddProductTotalAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "total_amount",
            table: "products",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE products
            SET total_amount = ROUND(purchase_price * (1 + (cgst + sgst) / 100), 2)
            """);

        migrationBuilder.AlterColumn<decimal>(
            name: "total_amount",
            table: "products",
            type: "numeric(18,2)",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "total_amount", table: "products");
}

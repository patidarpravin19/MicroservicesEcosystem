using AccountingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingInventory.Infrastructure.Migrations.AccountingInventoryDb;

[DbContext(typeof(AccountingInventoryDbContext))]
[Migration("20261007160000_AddSalesBillNumbers")]
public sealed class AddSalesBillNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bill_number",
            table: "sales_products",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.Sql("""
            WITH numbered_sales AS (
                SELECT id,
                       EXTRACT(YEAR FROM sale_date)::integer AS bill_year,
                       ROW_NUMBER() OVER (
                           PARTITION BY EXTRACT(YEAR FROM sale_date)
                           ORDER BY sale_date, created_at, id) AS bill_sequence
                FROM sales_products
            )
            UPDATE sales_products AS sale
            SET bill_number = 'SM-' || numbered_sales.bill_year::text || '-' ||
                LPAD(numbered_sales.bill_sequence::text, 6, '0')
            FROM numbered_sales
            WHERE sale.id = numbered_sales.id;

            CREATE TABLE sales_bill_counters (
                bill_year integer PRIMARY KEY,
                last_number integer NOT NULL
            );

            INSERT INTO sales_bill_counters (bill_year, last_number)
            SELECT EXTRACT(YEAR FROM sale_date)::integer, COUNT(*)::integer
            FROM sales_products
            GROUP BY EXTRACT(YEAR FROM sale_date)::integer;

            CREATE OR REPLACE FUNCTION generate_sales_bill_number(target_year integer)
            RETURNS text
            LANGUAGE sql
            AS $$
                INSERT INTO sales_bill_counters (bill_year, last_number)
                VALUES (target_year, 1)
                ON CONFLICT (bill_year) DO UPDATE
                    SET last_number = sales_bill_counters.last_number + 1
                RETURNING 'SM-' || target_year::text || '-' ||
                    CASE WHEN last_number < 1000000
                        THEN LPAD(last_number::text, 6, '0')
                        ELSE last_number::text
                    END;
            $$;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "bill_number",
            table: "sales_products",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_sales_products_bill_number",
            table: "sales_products",
            column: "bill_number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("ix_sales_products_bill_number", "sales_products");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS generate_sales_bill_number(integer); DROP TABLE IF EXISTS sales_bill_counters;");
        migrationBuilder.DropColumn("bill_number", "sales_products");
    }
}

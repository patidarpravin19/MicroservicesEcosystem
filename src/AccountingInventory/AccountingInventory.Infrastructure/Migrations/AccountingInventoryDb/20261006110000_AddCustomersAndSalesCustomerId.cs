using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingInventory.Infrastructure.Migrations.AccountingInventoryDb;

public sealed partial class AddCustomersAndSalesCustomerId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("customers", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by = table.Column<Guid>(type: "uuid", nullable: true),
            modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            is_active = table.Column<bool>(type: "boolean", nullable: false),
            is_deleted = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_customers", x => x.id));
        migrationBuilder.CreateIndex("ix_customers_mobile", "customers", "mobile", unique: true);
        migrationBuilder.Sql("""
            INSERT INTO customers (id, name, mobile, address, email, created_at, created_by, modified_at, modified_by, is_active, is_deleted)
            SELECT DISTINCT ON (customer_mobile) gen_random_uuid(), customer_name, customer_mobile, customer_address,
                   NULL, now(), NULL, NULL, NULL, TRUE, FALSE
            FROM sales_products ORDER BY customer_mobile, sale_date, id
            """);

        migrationBuilder.AddColumn<Guid>("customer_id", "sales_products", type: "uuid", nullable: true);
        migrationBuilder.Sql("""
            UPDATE sales_products AS sale SET customer_id = customer.id
            FROM customers AS customer WHERE customer.mobile = sale.customer_mobile
            """);
        migrationBuilder.AlterColumn<Guid>("customer_id", "sales_products", type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.CreateIndex("ix_sales_products_customer_id", "sales_products", "customer_id");
        migrationBuilder.AddForeignKey("fk_sales_products_customers_customer_id", "sales_products", "customer_id", "customers", principalColumn: "id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn("customer_name", "sales_products");
        migrationBuilder.DropColumn("customer_mobile", "sales_products");
        migrationBuilder.DropColumn("customer_address", "sales_products");
        migrationBuilder.DropIndex("ix_sales_products_product_id", "sales_products");
        ReorderSalesProductAuditColumns(migrationBuilder);
        migrationBuilder.CreateIndex("ix_sales_products_product_id", "sales_products", "product_id", unique: true, filter: "is_deleted = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_sales_products_customers_customer_id", "sales_products");
        migrationBuilder.DropIndex("ix_sales_products_customer_id", "sales_products");
        migrationBuilder.AddColumn<string>("customer_name", "sales_products", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("customer_mobile", "sales_products", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>("customer_address", "sales_products", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.Sql("""
            UPDATE sales_products AS sale SET customer_name = customer.name, customer_mobile = customer.mobile, customer_address = customer.address
            FROM customers AS customer WHERE customer.id = sale.customer_id
            """);
        migrationBuilder.AlterColumn<string>("customer_name", "sales_products", type: "character varying(200)", maxLength: 200, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200, oldNullable: true);
        migrationBuilder.AlterColumn<string>("customer_mobile", "sales_products", type: "character varying(20)", maxLength: 20, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20, oldNullable: true);
        migrationBuilder.AlterColumn<string>("customer_address", "sales_products", type: "character varying(500)", maxLength: 500, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(500)", oldMaxLength: 500, oldNullable: true);
        migrationBuilder.DropColumn("customer_id", "sales_products");
        migrationBuilder.DropIndex("ix_sales_products_product_id", "sales_products");
        ReorderSalesProductAuditColumns(migrationBuilder);
        migrationBuilder.CreateIndex("ix_sales_products_product_id", "sales_products", "product_id", unique: true, filter: "is_deleted = false");
        migrationBuilder.DropTable("customers");
    }

    private static void ReorderSalesProductAuditColumns(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("""
            ALTER TABLE sales_products ADD COLUMN _reorder_created_at timestamp with time zone;
            ALTER TABLE sales_products ADD COLUMN _reorder_created_by uuid;
            ALTER TABLE sales_products ADD COLUMN _reorder_modified_at timestamp with time zone;
            ALTER TABLE sales_products ADD COLUMN _reorder_modified_by uuid;
            ALTER TABLE sales_products ADD COLUMN _reorder_is_active boolean;
            ALTER TABLE sales_products ADD COLUMN _reorder_is_deleted boolean;
            UPDATE sales_products SET
                _reorder_created_at = created_at, _reorder_created_by = created_by,
                _reorder_modified_at = modified_at, _reorder_modified_by = modified_by,
                _reorder_is_active = is_active, _reorder_is_deleted = is_deleted;
            ALTER TABLE sales_products DROP COLUMN created_at, DROP COLUMN created_by,
                DROP COLUMN modified_at, DROP COLUMN modified_by, DROP COLUMN is_active, DROP COLUMN is_deleted;
            ALTER TABLE sales_products ADD COLUMN created_at timestamp with time zone NULL;
            ALTER TABLE sales_products ADD COLUMN created_by uuid NULL;
            ALTER TABLE sales_products ADD COLUMN modified_at timestamp with time zone NULL;
            ALTER TABLE sales_products ADD COLUMN modified_by uuid NULL;
            ALTER TABLE sales_products ADD COLUMN is_active boolean NULL;
            ALTER TABLE sales_products ADD COLUMN is_deleted boolean NULL;
            UPDATE sales_products SET
                created_at = _reorder_created_at, created_by = _reorder_created_by,
                modified_at = _reorder_modified_at, modified_by = _reorder_modified_by,
                is_active = _reorder_is_active, is_deleted = _reorder_is_deleted;
            ALTER TABLE sales_products ALTER COLUMN created_at SET NOT NULL;
            ALTER TABLE sales_products ALTER COLUMN is_active SET NOT NULL;
            ALTER TABLE sales_products ALTER COLUMN is_deleted SET NOT NULL;
            ALTER TABLE sales_products DROP COLUMN _reorder_created_at, DROP COLUMN _reorder_created_by,
                DROP COLUMN _reorder_modified_at, DROP COLUMN _reorder_modified_by,
                DROP COLUMN _reorder_is_active, DROP COLUMN _reorder_is_deleted;
            """);
}

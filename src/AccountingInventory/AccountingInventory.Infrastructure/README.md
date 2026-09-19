# To Migrate tables in database
1. Open the terminal and navigate to the project directory. e.g. cd src/AccountingInventory
2. Run the following command to apply migrations:
		dotnet ef migrations add InitialCreate --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context TenantDBContext
		OR
		# To Remove the migration if needed
		dotnet ef migrations remove --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context TenantDBContext
3. update database with the following command:
		dotnet ef database update --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context TenantDBContext

# To Migrate tables in Accounting and Inventory DB Context

1. Open the terminal and navigate to the project directory. e.g. src/Identity
2. Run the following command to apply migrations:
		dotnet ef migrations add InitialCreate --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context AccountingInventoryDbContext
		OR
		# To Remove the migration if needed
		dotnet ef migrations remove --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context AccountingInventoryDbContext
3. update database with the following command:
		dotnet ef database update --project AccountingInventory.Infrastructure --startup-project AccountingInventory.API --context AccountingInventoryDbContext


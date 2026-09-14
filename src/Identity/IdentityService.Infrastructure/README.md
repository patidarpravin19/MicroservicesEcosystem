# To Migrate tables in database
1. Open the terminal and navigate to the project directory. e.g. src/Identity
2. Run the following command to apply migrations:
		dotnet ef migrations add InitialCreate --project IdentityService.Infrastructure --startup-project IdentityService.API --context TenantDBContext
		OR
		# To Remove the migration if needed
		dotnet ef migrations remove --project IdentityService.Infrastructure --startup-project IdentityService.API --context TenantDBContext
3. update database with the following command:
		dotnet ef database update --project IdentityService.Infrastructure --startup-project IdentityService.API --context TenantDBContext
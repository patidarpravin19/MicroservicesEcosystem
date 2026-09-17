using AccountingInventory.Infrastructure.Persistence.MultiTenancy;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace AccountingInventory.Infrastructure.Persistence;

internal static class TenantSchemaMigrator
{
    public static async Task MigrateAsync(
        string connectionString,
        string schemaName,
        CancellationToken cancellationToken)
    {
        TenantSchemaNameValidator.EnsureValid(schemaName);
        var tenantProvider = new StaticTenantProvider(schemaName);
        var tenantConnectionString = CreateTenantConnectionString(connectionString, schemaName);

        var options = new DbContextOptionsBuilder<AccountingInventoryDbContext>()
            .UseNpgsql(
                tenantConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__TenantSchemaHistory", tenantProvider.SchemaName))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
            .Options;

        await using var applicationDbContext = new AccountingInventoryDbContext(options, tenantProvider);

        // PostgreSQL identifiers cannot be parameters. Validation guarantees this
        // value is a valid tenant-schema identifier before it is interpolated.
        await applicationDbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS \"{tenantProvider.SchemaName}\";", cancellationToken);
        await applicationDbContext.Database.MigrateAsync(cancellationToken);
    }

    internal static string CreateTenantConnectionString(string connectionString, string schemaName)
    {
        TenantSchemaNameValidator.EnsureValid(schemaName);
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            // Include public for extensions/functions while ensuring unqualified
            // EF DDL and DML always resolve to this tenant first.
            SearchPath = $"{schemaName},public"
        };

        return builder.ConnectionString;
    }
}

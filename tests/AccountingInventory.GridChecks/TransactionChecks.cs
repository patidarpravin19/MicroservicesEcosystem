using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using AccountingInventory.Infrastructure.Persistence;
using AccountingInventory.Infrastructure.Persistence.MultiTenancy;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

internal static class TransactionChecks
{
    public static async Task RunAsync(string connectionString)
    {
        // A committed scratch schema is necessary for two independent sessions.
        // It is never a tenant schema and is removed in finally.
        var schema = "transaction_check_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connectionString);
        await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA {schema}", control))
            await create.ExecuteNonQueryAsync();
        try
        {
            var isolated = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema }.ConnectionString;
            var options = new DbContextOptionsBuilder<AccountingInventoryDbContext>().UseNpgsql(isolated)
                .UseSnakeCaseNamingConvention().Options;
            var provider = new ProbeTenant(schema);
            var owner = User.Create("owner", "9000000000", "owner@example.com");
            owner.SetPasswordHash("test-only");
            await using (var setup = new AccountingInventoryDbContext(options, provider))
            {
                await setup.Database.ExecuteSqlRawAsync(setup.Database.GenerateCreateScript());
                setup.Users.Add(owner); await setup.SaveChangesAsync();
                await setup.Database.ExecuteSqlRawAsync("CREATE TABLE concurrency_balance (id integer PRIMARY KEY, paid numeric(18,2) NOT NULL); INSERT INTO concurrency_balance VALUES (1, 0)");
            }
            var arrivals = 0;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> Attempt(string code)
            {
                await using var db = new AccountingInventoryDbContext(options, provider);
                var behavior = new BusinessTransactionBehavior<BalanceProbeCommand, int>(db, new ProbeIdentity(owner.Id));
                try
                {
                    await behavior.Handle(new(), async cancellationToken =>
                    {
                        var paid = await db.Database.SqlQueryRaw<decimal>("SELECT paid AS \"Value\" FROM concurrency_balance WHERE id = 1").SingleAsync(cancellationToken);
                        if (paid + 80 > 100) throw new ConflictException("Overpayment");
                        if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
                        await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
                        db.ChartAccounts.Add(ChartAccount.Create(code, "Atomic write probe", LedgerAccountType.Asset, LedgerBalanceSide.Debit));
                        await db.SaveChangesAsync(cancellationToken);
                        await db.Database.ExecuteSqlRawAsync("UPDATE concurrency_balance SET paid = paid + 80 WHERE id = 1", cancellationToken);
                        return 1;
                    }, default);
                    return true;
                }
                catch (ConflictException) { return false; }
            }
            var outcomes = await Task.WhenAll(Attempt("PROBE-A"), Attempt("PROBE-B"));
            await using var verification = new AccountingInventoryDbContext(options, provider);
            var paid = await verification.Database.SqlQueryRaw<decimal>("SELECT paid AS \"Value\" FROM concurrency_balance WHERE id = 1").SingleAsync();
            if (outcomes.Count(success => success) != 1 || paid != 80 || await verification.ChartAccounts.CountAsync() != 1)
                throw new Exception("Serializable writes must reject a concurrent overpayment and roll back every write from the rejected request.");
            var unauthorized = new BusinessTransactionBehavior<BalanceProbeCommand, int>(verification, new ProbeIdentity(Guid.NewGuid()));
            var invoked = false;
            try
            {
                await unauthorized.Handle(new(), ct => { invoked = true; return Task.FromResult(1); }, default);
                throw new Exception("A staff member without a grant must be denied.");
            }
            catch (ForbiddenException) { }
            if (invoked) throw new Exception("Unauthorized handlers must not run.");
            Console.WriteLine("PASS: concurrent transaction conflict, complete rollback and pipeline authorization.");
        }
        finally
        {
            // schema is generated above from a fixed prefix and a GUID, never user input.
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", control);
            await cleanup.ExecuteNonQueryAsync();
        }
    }
    private sealed record BalanceProbeCommand : IRequest<int>;
    private sealed class ProbeTenant(string schema) : ITenantProvider { public string SchemaName => schema; }
    private sealed class ProbeIdentity(Guid userId) : IRequestIdentity { public Guid? UserId => userId; }
}

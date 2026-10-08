using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.GeneralLedger;

public sealed record OpeningBalanceLineInput(Guid AccountId, decimal Debit, decimal Credit, string? Memo = null);
public sealed record ImportOpeningBalancesCommand(DateOnly CutoverDate, IReadOnlyCollection<OpeningBalanceLineInput> Lines)
    : IRequest<JournalSummary>;

public sealed class ImportOpeningBalancesValidator : AbstractValidator<ImportOpeningBalancesCommand>
{
    public ImportOpeningBalancesValidator()
    {
        RuleFor(command => command.CutoverDate).NotEmpty();
        RuleFor(command => command.Lines).NotNull().Must(lines => lines.Count > 0)
            .WithMessage("Add at least one opening account balance.");
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.AccountId).NotEmpty();
            line.RuleFor(item => item.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(item => item.Credit).GreaterThanOrEqualTo(0);
            line.RuleFor(item => item.Memo).MaximumLength(250);
        });
        RuleFor(command => command).Must(command => command.Lines != null && command.Lines.All(line =>
            (decimal.Round(line.Debit, 2, MidpointRounding.AwayFromZero) > 0)
            != (decimal.Round(line.Credit, 2, MidpointRounding.AwayFromZero) > 0)))
            .WithMessage("Each opening balance must have a positive debit or credit amount.");
    }
}

/// <summary>Imports balance-sheet opening balances and posts the difference to retained earnings.</summary>
public sealed class ImportOpeningBalancesHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<ImportOpeningBalancesCommand, JournalSummary>
{
    public async Task<JournalSummary> Handle(ImportOpeningBalancesCommand request, CancellationToken ct)
    {
        var sourceId = request.CutoverDate.ToString("yyyy-MM-dd");
        if (await db.JournalEntries.AnyAsync(entry => entry.SourceType == "OpeningBalances" && entry.SourceId == sourceId, ct))
            throw new ConflictException("Opening balances have already been imported for this cutover date.");
        await LedgerPosting.EnsurePeriodOpenAsync(db, request.CutoverDate, ct);

        var rounded = request.Lines.Select(line => (line.AccountId,
            Debit: decimal.Round(line.Debit, 2, MidpointRounding.AwayFromZero),
            Credit: decimal.Round(line.Credit, 2, MidpointRounding.AwayFromZero),
            Memo: string.IsNullOrWhiteSpace(line.Memo) ? null : line.Memo.Trim())).ToArray();
        var ids = rounded.Select(line => line.AccountId).Append(Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToArray();
        var accounts = await db.ChartAccounts.AsNoTracking().Where(account => ids.Contains(account.Id) && account.IsActive)
            .ToDictionaryAsync(account => account.Id, ct);
        if (accounts.Count != ids.Length) throw new ConflictException("Every opening balance must reference an active account.");
        if (accounts.Values.Any(account => account.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense))
            throw new ConflictException("Opening balances must use balance-sheet accounts; import prior earnings to retained earnings.");

        var debit = rounded.Sum(line => line.Debit);
        var credit = rounded.Sum(line => line.Credit);
        var difference = decimal.Round(debit - credit, 2, MidpointRounding.AwayFromZero);
        if (difference != 0)
        {
            var retained = await db.ChartAccounts.SingleOrDefaultAsync(account => account.Code == "3100" && account.IsActive, ct)
                ?? throw new ConflictException("Initialize the chart of accounts first; retained earnings account 3100 is required.");
            rounded = rounded.Append((retained.Id, Debit: difference < 0 ? -difference : 0m,
                Credit: difference > 0 ? difference : 0m, Memo: (string?)"Opening balance offset")).ToArray();
            accounts[retained.Id] = retained;
        }

        var entry = JournalEntry.Post(request.CutoverDate, "Opening balance cutover", "OpeningBalances", sourceId,
            rounded.Select(line => (line.AccountId, line.Debit, line.Credit, line.Memo)).ToArray());
        db.JournalEntries.Add(entry);
        db.JournalLines.AddRange(entry.Lines);
        await db.SaveChangesAsync(ct);
        var result = entry.Lines.Select(line => new JournalLineSummary(line.AccountId, accounts[line.AccountId].Code,
            accounts[line.AccountId].Name, line.Debit, line.Credit, line.Memo)).ToArray();
        return new(entry.Id, entry.JournalNumber, entry.JournalDate, entry.Description, entry.SourceType,
            entry.SourceId, entry.PostedAt, null, false, result, result.Sum(line => line.Debit), result.Sum(line => line.Credit));
    }
}

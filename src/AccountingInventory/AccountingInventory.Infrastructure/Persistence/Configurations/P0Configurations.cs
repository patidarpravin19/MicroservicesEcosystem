using AccountingInventory.Domain.Entities;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Infrastructure.Persistence.Configurations;

internal static class P0Configurations
{
    public static void Configure(ModelBuilder model)
    {
        Configure<InvoiceCorrection>(model, "invoice_corrections");
        Configure<CorrectionRefund>(model, "correction_refunds");
        Configure<InvoiceSnapshot>(model, "invoice_snapshots");
        Configure<OpeningSubledgerBalance>(model, "opening_subledger_balances");
        Configure<OpeningSettlement>(model, "opening_settlements");
        model.Entity<InvoiceCorrection>().HasIndex(x => new { x.Kind, x.SourceId }).IsUnique();
        model.Entity<InvoiceCorrection>().HasIndex(x => x.NoteNumber).IsUnique();
        model.Entity<InvoiceSnapshot>().HasIndex(x => new { x.Kind, x.SourceId }).IsUnique();
        model.Entity<InvoiceSnapshot>().Property(x => x.DetailsJson).HasColumnType("jsonb");
        model.Entity<CorrectionRefund>().HasOne<InvoiceCorrection>().WithMany().HasForeignKey(x => x.CorrectionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OpeningSubledgerBalance>().HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OpeningSubledgerBalance>().HasIndex(x => new { x.Kind, x.PartyId, x.Reference }).IsUnique();
        model.Entity<OpeningSettlement>().HasOne<OpeningSubledgerBalance>().WithMany().HasForeignKey(x => x.OpeningBalanceId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<User>().HasIndex(x => x.IsOwner).IsUnique().HasFilter("is_owner = true");
        model.Entity<User>().Property(x => x.InvitationHash).HasMaxLength(64);
        model.Entity<SalesProduct>().HasIndex(x => x.ProductId).IsUnique().HasFilter("is_deleted = false AND is_returned = false");
    }
    private static void Configure<T>(ModelBuilder model, string table) where T : AggregateRoot
    {
        var builder = model.Entity<T>();
        builder.ToTable(table); builder.HasKey(x => x.Id); builder.Ignore(x => x.DomainEvents);
        builder.HasQueryFilter(x => !x.IsDeleted);
        foreach (var property in typeof(T).GetProperties().Where(x => x.PropertyType == typeof(decimal)))
            builder.Property(property.Name).HasPrecision(18, 2);
    }
}

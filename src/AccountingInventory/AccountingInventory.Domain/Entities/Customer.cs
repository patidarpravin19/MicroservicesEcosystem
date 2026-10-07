using BuildingBlocks.Domain;

namespace AccountingInventory.Domain.Entities;

/// <summary>Customer contact details used by sales in the current tenant.</summary>
public sealed class Customer : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Mobile { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string? Email { get; private set; }

    public static Customer Create(string name, string mobile, string address, string? email)
        => new()
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Name = name.Trim(),
            Mobile = mobile.Trim(),
            Address = address.Trim(),
            Email = NormalizeEmail(email)
        };

    public void UpdateContactDetails(string name, string mobile, string address, string? email)
    {
        Name = name.Trim();
        Mobile = mobile.Trim();
        Address = address.Trim();
        Email = NormalizeEmail(email);
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim();

    private Customer() { }
}

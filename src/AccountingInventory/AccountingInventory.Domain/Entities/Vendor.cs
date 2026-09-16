using BuildingBlocks.Domain;
using AccountingInventory.Domain.Exceptions;

namespace AccountingInventory.Domain.Entities;

/// <summary>
/// A database-managed vendor, scoped to one tenant. This is the ENTIRE mechanism behind
/// "which vendors can access which screen/controller": a Vendor simply owns a list of
/// permission codes (e.g. "Inventory.StockItems.Create"), editable at any time via
/// VendorEndpoints without touching code or redeploying anything. Every user assigned
/// this vendor picks up its current permission codes as JWT claims the next time they
/// log in or refresh their token (see LoginCommandHandler / RefreshTokenCommandHandler).
/// </summary>
public sealed class Vendor : AggregateRoot
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Mobile { get; init; }
    public required string Email { get; init; }
    public string? Description { get; init; }
    public string? Address { get; init; }

    public static Vendor Create(string name, string code, string mobile, string email,
        string description = null, string address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AccountingInventoryDomainException("Vendor name cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AccountingInventoryDomainException("Vendor code cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new AccountingInventoryDomainException("Vendor mobile cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new AccountingInventoryDomainException("Vendor email cannot be empty.");
        }
        return new Vendor
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Code = code.Trim(),
            Mobile = mobile.Trim(),
            Email = email.Trim(),
            Description = description?.Trim(),
            Address = address?.Trim()
        };
    }

    public void Delete()
    {
        IsDeleted = true;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void InActivate()
    {
        IsActive = false;
    }

    private Vendor() { }
}

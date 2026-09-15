using BuildingBlocks.Domain;
using AccountingInventory.Domain.Exceptions;

namespace AccountingInventory.Domain.Entities;

/// <summary>
/// A database-managed role, scoped to one tenant. This is the ENTIRE mechanism behind
/// "which roles can access which screen/controller": a Role simply owns a list of
/// permission codes (e.g. "Inventory.StockItems.Create"), editable at any time via
/// RoleEndpoints without touching code or redeploying anything. Every user assigned
/// this role picks up its current permission codes as JWT claims the next time they
/// log in or refresh their token (see LoginCommandHandler / RefreshTokenCommandHandler).
/// </summary>
public sealed class Role : AggregateRoot, ITenantEntity
{
    private readonly List<string> _permissionCodes = [];

    public Guid TenantId { get; set; }
    public required string Name { get; init; }
    public bool IsSystemDefined { get; private set; }
    public IReadOnlyCollection<string> PermissionCodes => _permissionCodes.AsReadOnly();

    public static Role Create(Guid tenantId, string name, bool isSystemDefined = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AccountingInventoryDomainException("Role name cannot be empty.");
        }

        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            IsSystemDefined = isSystemDefined,
        };
    }

    public void GrantPermission(string permissionCode)
    {
        var normalized = NormalizePermissionCode(permissionCode);

        if (!_permissionCodes.Contains(normalized))
        {
            _permissionCodes.Add(normalized);
        }
    }

    public void RevokePermission(string permissionCode)
    {
        if (IsSystemDefined)
        {
            throw new AccountingInventoryDomainException($"Role '{Name}' is system-defined and cannot be modified.");
        }

        _permissionCodes.Remove(NormalizePermissionCode(permissionCode));
    }

    public void Delete()
    {
        if (IsSystemDefined)
        {
            throw new AccountingInventoryDomainException($"Role '{Name}' is system-defined and cannot be deleted.");
        }

        IsDeleted = true;
    }

    private static string NormalizePermissionCode(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new AccountingInventoryDomainException("Permission code cannot be empty.");
        }

        return permissionCode.Trim();
    }

    private Role() { }
}

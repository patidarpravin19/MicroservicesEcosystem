using MediatR;

namespace AccountingInventory.Application.Vendors.Commands.UpdateVendor;

/// <summary>Self-service brand update — deliberately anonymous at the API layer
/// this in a production deployment.</summary>
public sealed record UpdateBrandCommand(Guid Id, Guid VendorId, string Name, string Description) : IRequest<UpdateBrandResult>;

public sealed record UpdateBrandResult(Guid Id, Guid VendorId, string Name, string Description);

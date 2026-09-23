using MediatR;

namespace AccountingInventory.Application.Vendors.Queries.GetVendorById;

/// <summary>Used by a signup/login UI to check slug availability or display vendor
/// info before authentication.</summary>
public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandSummary>;

public sealed record BrandSummary(Guid Id, string Name, string Description);

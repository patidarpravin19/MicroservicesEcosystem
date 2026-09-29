using AccountingInventory.Application.Products.Commands.CreateProduct;
using AccountingInventory.Application.Products.Commands.DeleteProduct;
using AccountingInventory.Application.Products.Commands.UpdateProduct;
using AccountingInventory.Application.Products.Queries.GetProductById;
using AccountingInventory.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingInventory.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization("AuthenticatedUser")
            .AddEndpointFilter<TenantHeaderEndpointFilter>()
            .WithMetadata(new BuildingBlocks.WebDefaults.RequiresTenantIdHeaderAttribute());

        group.MapPost("/", async (CreateProductCommand command, ISender sender, CancellationToken ct) =>
            Results.Created("/api/products", await sender.Send(command, ct)))
            .WithName("CreateProduct");

        group.MapGet("/", async ([FromQuery] int? page, [FromQuery] int? pageSize,
            [FromQuery] string? search, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetProductsQuery(page ?? 1, pageSize ?? 20, search), ct)))
            .WithName("GetProducts");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetProductByIdQuery(id), ct)))
            .WithName("GetProductById");

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, ISender sender, CancellationToken ct) =>
        {
            if (id != command.Id) return Results.BadRequest("ID in route does not match ID in body.");
            return Results.Ok(await sender.Send(command, ct));
        })
            .WithName("UpdateProduct");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteProductCommand(id), ct);
            return Results.NoContent();
        }).WithName("DeleteProduct");

        return group;
    }
}

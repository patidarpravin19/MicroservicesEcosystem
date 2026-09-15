using System.Security.Claims;
using AccountingInventory.Application.Users.Commands.Login;
using AccountingInventory.Application.Users.Commands.Refresh;
using AccountingInventory.Application.Users.Commands.Register;
using MediatR;

namespace AccountingInventory.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/auth/users/{result.UserId}", result);
            })
            .WithName("Register").AllowAnonymous()
            .Produces<RegisterResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .WithName("Login").AllowAnonymous()
            .Produces<LoginResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .WithName("RefreshToken").AllowAnonymous()
            .Produces<RefreshTokenResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", (HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
                var permissions = httpContext.User.FindAll("permission").Select(c => c.Value);
                var tenantId = httpContext.User.FindFirstValue("tenant_id");

                return Results.Ok(new { userId, tenantId, roles, permissions });
            })
            .WithName("Me")
            .RequireAuthorization("AuthenticatedUser");

        return group;
    }
}

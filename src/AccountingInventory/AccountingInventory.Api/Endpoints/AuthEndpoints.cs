using System.Security.Claims;
using AccountingInventory.Application.Users.Commands.Login;
using AccountingInventory.Application.Users.Commands.Refresh;
using AccountingInventory.Application.Users.Commands.Register;
using BuildingBlocks.WebDefaults;
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
            .WithName("Register")
            .AllowAnonymous()
            // Registration has no access token yet, but still needs a tenant. This
            // filter resolves X-Tenant-Id to the registry-owned schema before the
            // MediatR handler resolves its tenant DbContext.
            .AddEndpointFilter<TenantHeaderEndpointFilter>()
            .WithMetadata(new RequiresTenantIdHeaderAttribute())
            .Produces<RegisterResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .WithName("Login").AllowAnonymous()
            //.AddEndpointFilter<TenantHeaderEndpointFilter>()
            //.WithMetadata(new RequiresTenantIdHeaderAttribute())
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
            .RequireAuthorization("AuthenticatedUser")
            // Unlike login/register/refresh, this is a tenant-scoped operation.
            // Resolve the schema from the tenant registry rather than trusting the
            // schema claim embedded in the access token.
            .AddEndpointFilter<TenantHeaderEndpointFilter>();

        return group;
    }
}

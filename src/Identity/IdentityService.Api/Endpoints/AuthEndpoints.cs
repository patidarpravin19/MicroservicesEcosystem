using System.Security.Claims;
using IdentityService.Application.Users.Commands.Login;
using IdentityService.Application.Users.Commands.Refresh;
using IdentityService.Application.Users.Commands.Register;
using MediatR;

namespace IdentityService.Api.Endpoints;

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
            .Produces<RegisterResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .WithName("Login")
            .Produces<LoginResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(command, ct)))
            .WithName("RefreshToken")
            .Produces<RefreshTokenResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", (HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                    ?? httpContext.User.Identity?.Name;
                var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);

                return Results.Ok(new { userId, userName, roles });
            })
            .WithName("Me")
            .RequireAuthorization("AuthenticatedUser");

        return group;
    }
}

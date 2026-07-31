using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.DTOs;
using DirectRide.Api.Models;
using DirectRide.Api.Services;

namespace DirectRide.Api.Controllers;

public static class UserController
{
    public static IEndpointRouteBuilder MapUserController(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");

        group.MapGet("/test", () =>
        {
            return new User
            {
                FirstName = "Sample",
                LastName = "Driver",
                Email = "sample.driver@directride.com",
                PhoneNumber = "555-555-5555",
                Role = UserRole.Driver
            };
        });

        group.MapGet("/me", async (
            ClaimsPrincipal claimsPrincipal,
            UserService userService,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(claimsPrincipal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await userService.GetUserAsync(userId, cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization();

        group.MapGet("", async (
            UserService userService,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? role = null,
            string? status = null) =>
        {
            var users = await userService.GetUsersAsync(
                page,
                pageSize,
                search,
                role,
                status,
                cancellationToken);

            return Results.Ok(users);
        })
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (
            UserService userService,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.GetUserAsync(id, cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization();

        group.MapPost("", async (
            UserService userService,
            CreateUserDto dto,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.CreateUserAsync(dto, cancellationToken);

            return result.Status == UserServiceResultStatus.Created
                ? Results.Created($"/users/{result.Value?.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapPut("/{id:guid}", async (
            UserService userService,
            Guid id,
            UpdateUserDto dto,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.UpdateUserAsync(id, dto, cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization();

        group.MapPatch("/{id:guid}", async (
            UserService userService,
            Guid id,
            PatchUserDto dto,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.PatchUserAsync(id, dto, cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization();

        return app;
    }

    private static IResult ToHttpResult<T>(UserServiceResult<T> result)
    {
        return result.Status switch
        {
            UserServiceResultStatus.Ok => Results.Ok(result.Value),
            UserServiceResultStatus.NotFound => Results.NotFound(result.Error),
            UserServiceResultStatus.Created => Results.Created(string.Empty, result.Value),
            _ => Results.Problem("Unexpected user service result.")
        };
    }

    private static bool TryGetUserId(ClaimsPrincipal claimsPrincipal, out Guid userId)
    {
        var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdClaim, out userId);
    }
}

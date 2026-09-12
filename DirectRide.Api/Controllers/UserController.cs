using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.DTOs;
using DirectRide.Api.Models;
using DirectRide.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DirectRide.Api.Controllers;

public static class UserController
{
    private const long MaxProfilePhotoRequestSize = UserService.MaxProfilePhotoSize + (1024 * 1024);

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

        group.MapPut("/{id:guid}/profile-photo", async (
            ClaimsPrincipal claimsPrincipal,
            UserService userService,
            Guid id,
            IFormFile file,
            CancellationToken cancellationToken) =>
        {
            var authorizationResult = AuthorizeProfilePhotoChange(claimsPrincipal, id);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var result = await userService.UploadProfilePhotoAsync(
                id,
                memoryStream.ToArray(),
                file.ContentType,
                cancellationToken);

            return ToHttpResult(result);
        })
        .WithMetadata(new RequestSizeLimitAttribute(MaxProfilePhotoRequestSize))
        .DisableAntiforgery()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}/profile-photo", async (
            ClaimsPrincipal claimsPrincipal,
            UserService userService,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var authorizationResult = AuthorizeProfilePhotoChange(claimsPrincipal, id);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var result = await userService.DeleteProfilePhotoAsync(
                id,
                cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization();

        return app;
    }

    private static IResult? AuthorizeProfilePhotoChange(
        ClaimsPrincipal claimsPrincipal,
        Guid userId)
    {
        if (claimsPrincipal.IsInRole(UserRole.Admin.ToString()))
        {
            return null;
        }

        if (!TryGetUserId(claimsPrincipal, out var authenticatedUserId))
        {
            return Results.Unauthorized();
        }

        return authenticatedUserId == userId
            ? null
            : Results.Forbid();
    }

    private static IResult ToHttpResult<T>(UserServiceResult<T> result)
    {
        return result.Status switch
        {
            UserServiceResultStatus.Ok => Results.Ok(result.Value),
            UserServiceResultStatus.BadRequest => Results.BadRequest(result.Error),
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

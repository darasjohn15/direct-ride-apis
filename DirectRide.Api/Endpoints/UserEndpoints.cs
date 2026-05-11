using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.Data;
using DirectRide.Api.DTOs;
using DirectRide.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");

        group.MapGet("/test", () =>
        {
            var user = new User
            {
                FirstName = "Razzo",
                LastName = "Driver",
                Email = "razzo@directride.com",
                PhoneNumber = "555-555-5555",
                Role = UserRole.Driver
            };

            return user;
        });

        group.MapGet("/me", async (ClaimsPrincipal claimsPrincipal, AppDbContext db) =>
        {
            var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role.ToString(),
                    CreatedAt = u.CreatedAt,
                    BaseFare = u.BaseFare,
                })
                .FirstOrDefaultAsync();

            return user is null
                ? Results.NotFound("User not found.")
                : Results.Ok(user);
        })
        .RequireAuthorization();

        group.MapGet("", async (AppDbContext db) =>
        {
            var users = await db.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role.ToString(),
                    CreatedAt = u.CreatedAt,
                    BaseFare = u.BaseFare,
                })
                .ToListAsync();

            return Results.Ok(users);
        })
        .RequireAuthorization();

        group.MapPost("", async (AppDbContext db, CreateUserDto dto, PasswordHasher<User> hasher) =>
        {
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = (UserRole)dto.Role
            };

            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            };

            return Results.Created($"/users/{user.Id}", response);
        });

        return app;
    }
}

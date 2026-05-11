using DirectRide.Api.Data;
using DirectRide.Api.DTOs.Auth;
using DirectRide.Api.Models;
using DirectRide.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", async (
            AppDbContext db,
            LoginDto dto,
            PasswordHasher<User> hasher,
            JwtService jwtService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            var token = jwtService.GenerateToken(user);

            return Results.Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    Role = user.Role.ToString()
                }
            });
        });

        return app;
    }
}
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

        group.MapPost("/register", async (
            AppDbContext db,
            RegisterDto dto,
            PasswordHasher<User> hasher,
            JwtService jwtService) =>
        {
            var email = dto.Email.Trim();

            if (string.IsNullOrWhiteSpace(dto.FirstName)
                || string.IsNullOrWhiteSpace(dto.LastName)
                || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(dto.PhoneNumber)
                || string.IsNullOrWhiteSpace(dto.Password))
            {
                return Results.BadRequest("First name, last name, email, phone number, and password are required.");
            }

            if (!Enum.IsDefined(typeof(UserRole), dto.Role))
            {
                return Results.BadRequest("Invalid user role.");
            }

            var emailExists = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());

            if (emailExists)
            {
                return Results.Conflict("Email is already registered.");
            }

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                PhoneNumber = dto.PhoneNumber.Trim(),
                Role = (UserRole)dto.Role
            };

            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var token = jwtService.GenerateToken(user);

            return Results.Created($"/users/{user.Id}", new
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

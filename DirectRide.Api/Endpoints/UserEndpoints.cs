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

        group.MapGet("", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? role = null,
            string? status = null) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim().ToLower()}%";
                query = query.Where(u =>
                    EF.Functions.Like((u.FirstName + " " + u.LastName).ToLower(), searchTerm) ||
                    EF.Functions.Like(u.Email.ToLower(), searchTerm) ||
                    EF.Functions.Like(u.PhoneNumber.ToLower(), searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(role) && !role.Equals("All Roles", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
                {
                    query = query.Where(u => u.Role == parsedRole);
                }
                else if (int.TryParse(role, out var roleValue) && Enum.IsDefined(typeof(UserRole), roleValue))
                {
                    query = query.Where(u => u.Role == (UserRole)roleValue);
                }
                else
                {
                    query = query.Where(_ => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Deactivated", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(_ => false);
                }
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var effectivePage = totalPages > 0 ? Math.Min(page, totalPages) : 1;

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenBy(u => u.Id)
                .Skip((effectivePage - 1) * pageSize)
                .Take(pageSize)
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

            return Results.Ok(new PaginatedResponseDto<UserResponseDto>
            {
                Items = users,
                Page = effectivePage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPreviousPage = effectivePage > 1,
                HasNextPage = effectivePage < totalPages
            });
        })
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (AppDbContext db, Guid id) =>
        {
            var user = await db.Users
                .Where(u => u.Id == id)
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

        group.MapPut("/{id:guid}", async (AppDbContext db, Guid id, UpdateUserDto dto) =>
        {
            var user = await db.Users.FindAsync(id);

            if (user is null)
            {
                return Results.NotFound("User not found.");
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Role = (UserRole)dto.Role;
            user.BaseFare = dto.BaseFare;

            await db.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                BaseFare = user.BaseFare,
            };

            return Results.Ok(response);
        })
        .RequireAuthorization();

        group.MapPatch("/{id:guid}", async (AppDbContext db, Guid id, PatchUserDto dto) =>
        {
            var user = await db.Users.FindAsync(id);

            if (user is null)
            {
                return Results.NotFound("User not found.");
            }

            if (dto.FirstName is not null)
            {
                user.FirstName = dto.FirstName;
            }

            if (dto.LastName is not null)
            {
                user.LastName = dto.LastName;
            }

            if (dto.Email is not null)
            {
                user.Email = dto.Email;
            }

            if (dto.PhoneNumber is not null)
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            if (dto.Role.HasValue)
            {
                user.Role = (UserRole)dto.Role.Value;
            }

            if (dto.BaseFare.HasValue)
            {
                user.BaseFare = dto.BaseFare.Value;
            }

            await db.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                BaseFare = user.BaseFare,
            };

            return Results.Ok(response);
        })
        .RequireAuthorization();

        return app;
    }
}

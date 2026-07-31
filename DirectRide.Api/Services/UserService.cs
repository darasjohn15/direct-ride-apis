using DirectRide.Api.Data;
using DirectRide.Api.DTOs;
using DirectRide.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher;

    public UserService(AppDbContext db, PasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<UserServiceResult<UserResponseDto>> GetUserAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user is null
            ? UserServiceResult<UserResponseDto>.NotFound("User not found.")
            : UserServiceResult<UserResponseDto>.Ok(ToResponseDto(user));
    }

    public async Task<PaginatedResponseDto<UserResponseDto>> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users.AsQueryable();

        query = ApplyFilters(query, search, role, status);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var effectivePage = totalPages > 0 ? Math.Min(page, totalPages) : 1;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((effectivePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponseDto<UserResponseDto>
        {
            Items = users.Select(ToResponseDto).ToList(),
            Page = effectivePage,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = effectivePage > 1,
            HasNextPage = effectivePage < totalPages
        };
    }

    public async Task<UserServiceResult<UserResponseDto>> CreateUserAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Role = (UserRole)dto.Role
        };

        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return UserServiceResult<UserResponseDto>.Created(ToResponseDto(user));
    }

    public async Task<UserServiceResult<UserResponseDto>> UpdateUserAsync(
        Guid id,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);

        if (user is null)
        {
            return UserServiceResult<UserResponseDto>.NotFound("User not found.");
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.Role = (UserRole)dto.Role;
        user.BaseFare = dto.BaseFare;

        await _db.SaveChangesAsync(cancellationToken);

        return UserServiceResult<UserResponseDto>.Ok(ToResponseDto(user));
    }

    public async Task<UserServiceResult<UserResponseDto>> PatchUserAsync(
        Guid id,
        PatchUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);

        if (user is null)
        {
            return UserServiceResult<UserResponseDto>.NotFound("User not found.");
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

        await _db.SaveChangesAsync(cancellationToken);

        return UserServiceResult<UserResponseDto>.Ok(ToResponseDto(user));
    }

    private IQueryable<User> ApplyFilters(
        IQueryable<User> query,
        string? search,
        string? role,
        string? status)
    {
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

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase) &&
            status.Equals("Deactivated", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(_ => false);
        }

        return query;
    }

    private static UserResponseDto ToResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            BaseFare = user.BaseFare
        };
    }
}

public sealed record UserServiceResult<T>(
    UserServiceResultStatus Status,
    T? Value,
    string? Error)
{
    public static UserServiceResult<T> Ok(T value) =>
        new(UserServiceResultStatus.Ok, value, null);

    public static UserServiceResult<T> Created(T value) =>
        new(UserServiceResultStatus.Created, value, null);

    public static UserServiceResult<T> NotFound(string error) =>
        new(UserServiceResultStatus.NotFound, default, error);
}

public enum UserServiceResultStatus
{
    Ok,
    Created,
    NotFound
}

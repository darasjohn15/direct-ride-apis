using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.Data;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications")
            .RequireAuthorization();

        group.MapGet("", async (
            ClaimsPrincipal claimsPrincipal,
            AppDbContext db,
            [AsParameters] NotificationQueryDto query,
            [FromQuery(Name = "pageSize")] int? camelCasePageSize) =>
        {
            if (!TryGetUserId(claimsPrincipal, out var userId))
            {
                return Results.Unauthorized();
            }

            var page = Math.Max(query.Page ?? 1, 1);
            var pageSize = Math.Clamp(query.PageSize ?? camelCasePageSize ?? 20, 1, 100);

            var notificationsQuery = db.Notifications
                .Where(n => n.UserId == userId);

            var totalItems = await notificationsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var effectivePage = totalPages > 0 ? Math.Min(page, totalPages) : 1;

            var notifications = await notificationsQuery
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Skip((effectivePage - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    NotificationType = n.NotificationType.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    RideId = n.RideId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return Results.Ok(new PaginatedResponseDto<NotificationResponseDto>
            {
                Items = notifications,
                Page = effectivePage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPreviousPage = effectivePage > 1,
                HasNextPage = effectivePage < totalPages
            });
        });

        group.MapGet("/unread-count", async (ClaimsPrincipal claimsPrincipal, AppDbContext db) =>
        {
            if (!TryGetUserId(claimsPrincipal, out var userId))
            {
                return Results.Unauthorized();
            }

            var unreadCount = await db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Results.Ok(new UnreadNotificationCountResponseDto
            {
                UnreadCount = unreadCount
            });
        });

        group.MapPatch("/{notificationId:guid}/read", async (
            ClaimsPrincipal claimsPrincipal,
            AppDbContext db,
            Guid notificationId) =>
        {
            if (!TryGetUserId(claimsPrincipal, out var userId))
            {
                return Results.Unauthorized();
            }

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification is null)
            {
                return Results.NotFound("Notification not found.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok(ToResponse(notification));
        });

        group.MapPatch("/read-all", async (ClaimsPrincipal claimsPrincipal, AppDbContext db) =>
        {
            if (!TryGetUserId(claimsPrincipal, out var userId))
            {
                return Results.Unauthorized();
            }

            var unreadNotifications = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            var readAt = DateTime.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = readAt;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { updatedCount = unreadNotifications.Count });
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal claimsPrincipal, out Guid userId)
    {
        var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdClaim, out userId);
    }

    private static NotificationResponseDto ToResponse(Models.Notification notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id,
            NotificationType = notification.NotificationType.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            RideId = notification.RideId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}

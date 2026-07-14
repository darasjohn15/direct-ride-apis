using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.Data;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.Notifications;
using DirectRide.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectRide.Api.Tests;

public class NotificationsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnPaginatedNotifications_ForAuthenticatedUserOnly()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await SeedNotificationsAsync(
            new Notification
            {
                UserId = userId,
                NotificationType = NotificationType.RideAccepted,
                Title = "Newest",
                Message = "Newest message",
                CreatedAt = DateTime.UtcNow.AddMinutes(2)
            },
            new Notification
            {
                UserId = userId,
                NotificationType = NotificationType.RideRequested,
                Title = "Oldest",
                Message = "Oldest message",
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            },
            new Notification
            {
                UserId = otherUserId,
                NotificationType = NotificationType.RideDenied,
                Title = "Other user's notification",
                Message = "Should not be returned",
                CreatedAt = DateTime.UtcNow.AddMinutes(3)
            });

        var response = await SendAsync(HttpMethod.Get, "/notifications?page=1&page_size=1", userId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<NotificationResponseDto>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items[0].Title.Should().Be("Newest");
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(1);
        page.TotalItems.Should().Be(2);
        page.TotalPages.Should().Be(2);
        page.HasNextPage.Should().BeTrue();
        page.Items.Should().NotContain(n => n.Title == "Other user's notification");
    }

    [Fact]
    public async Task GetUnreadCount_ShouldCountOnlyAuthenticatedUsersUnreadNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await SeedNotificationsAsync(
            new Notification
            {
                UserId = userId,
                NotificationType = NotificationType.RideRequested,
                Title = "Unread",
                Message = "Unread message",
                IsRead = false
            },
            new Notification
            {
                UserId = userId,
                NotificationType = NotificationType.RideCompleted,
                Title = "Read",
                Message = "Read message",
                IsRead = true,
                ReadAt = DateTime.UtcNow
            },
            new Notification
            {
                UserId = otherUserId,
                NotificationType = NotificationType.RideCancelled,
                Title = "Other unread",
                Message = "Other unread message",
                IsRead = false
            });

        var response = await SendAsync(HttpMethod.Get, "/notifications/unread-count", userId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await response.Content.ReadFromJsonAsync<UnreadNotificationCountResponseDto>();
        count.Should().NotBeNull();
        count!.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task PatchNotificationRead_ShouldMarkAuthenticatedUsersNotificationAsRead()
    {
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            UserId = userId,
            NotificationType = NotificationType.RideRequested,
            Title = "Unread",
            Message = "Unread message",
            IsRead = false
        };

        await SeedNotificationsAsync(notification);

        var response = await SendAsync(HttpMethod.Patch, $"/notifications/{notification.Id}/read", userId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationResponseDto>();
        body.Should().NotBeNull();
        body!.IsRead.Should().BeTrue();
        body.ReadAt.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedNotification = await db.Notifications.SingleAsync(n => n.Id == notification.Id);
        storedNotification.IsRead.Should().BeTrue();
        storedNotification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PatchNotificationRead_ShouldReturnNotFound_ForAnotherUsersNotification()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notification = new Notification
        {
            UserId = otherUserId,
            NotificationType = NotificationType.RideRequested,
            Title = "Other unread",
            Message = "Other unread message",
            IsRead = false
        };

        await SeedNotificationsAsync(notification);

        var response = await SendAsync(HttpMethod.Patch, $"/notifications/{notification.Id}/read", userId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchNotificationsReadAll_ShouldMarkOnlyAuthenticatedUsersNotificationsAsRead()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var userNotification = new Notification
        {
            UserId = userId,
            NotificationType = NotificationType.RideRequested,
            Title = "User unread",
            Message = "User unread message",
            IsRead = false
        };
        var otherUserNotification = new Notification
        {
            UserId = otherUserId,
            NotificationType = NotificationType.RideRequested,
            Title = "Other unread",
            Message = "Other unread message",
            IsRead = false
        };

        await SeedNotificationsAsync(userNotification, otherUserNotification);

        var response = await SendAsync(HttpMethod.Patch, "/notifications/read-all", userId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var storedUserNotification = await db.Notifications.SingleAsync(n => n.Id == userNotification.Id);
        var storedOtherUserNotification = await db.Notifications.SingleAsync(n => n.Id == otherUserNotification.Id);

        storedUserNotification.IsRead.Should().BeTrue();
        storedUserNotification.ReadAt.Should().NotBeNull();
        storedOtherUserNotification.IsRead.Should().BeFalse();
        storedOtherUserNotification.ReadAt.Should().BeNull();
    }

    private async Task SeedNotificationsAsync(params Notification[] notifications)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userIds = notifications
            .Select(n => n.UserId)
            .Distinct()
            .ToList();

        var existingUserIds = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var users = userIds
            .Except(existingUserIds)
            .Select(userId => new User
            {
                Id = userId,
                FirstName = "Notification",
                LastName = "User",
                Email = $"{userId:N}@notifications.test",
                PhoneNumber = "555-000-0000",
                Role = UserRole.Rider,
                PasswordHash = "hashed-password"
            });

        db.Users.AddRange(users);
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Guid userId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId.ToString());

        return _client.SendAsync(request);
    }
}

using DirectRide.Api.Data;
using DirectRide.Api.Models;
using DirectRide.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task CreateNotificationAsync_ShouldStoreNotification()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            FirstName = "Riley",
            LastName = "Rider",
            Email = "riley@example.com",
            PhoneNumber = "555-0100",
            Role = UserRole.Rider,
            PasswordHash = "hashed-password"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new NotificationService(db);

        var notification = await service.CreateNotificationAsync(
            user.Id,
            NotificationType.RideRequested,
            "Ride requested",
            "Your ride request was sent.");

        var storedNotification = await db.Notifications.SingleAsync();
        storedNotification.Id.Should().Be(notification.Id);
        storedNotification.UserId.Should().Be(user.Id);
        storedNotification.NotificationType.Should().Be(NotificationType.RideRequested);
        storedNotification.Title.Should().Be("Ride requested");
        storedNotification.Message.Should().Be("Your ride request was sent.");
        storedNotification.RideId.Should().BeNull();
        storedNotification.IsRead.Should().BeFalse();
        storedNotification.ReadAt.Should().BeNull();
        storedNotification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

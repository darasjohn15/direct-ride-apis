using DirectRide.Api.Data;
using DirectRide.Api.Models;

namespace DirectRide.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Notification> CreateNotificationAsync(
        Guid userId,
        NotificationType notificationType,
        string title,
        string message,
        Guid? rideId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            NotificationType = notificationType,
            Title = title,
            Message = message,
            RideId = rideId
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        return notification;
    }
}

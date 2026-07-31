using DirectRide.Api.Models;
using DirectRide.Api.Repositories;

namespace DirectRide.Api.Services;

public class NotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
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

        _notifications.Add(notification);
        await _notifications.SaveChangesAsync(cancellationToken);

        return notification;
    }
}

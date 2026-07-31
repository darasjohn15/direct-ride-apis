using DirectRide.Api.Data;
using DirectRide.Api.Models;

namespace DirectRide.Api.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public void Add(Notification notification)
    {
        _db.Notifications.Add(notification);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}

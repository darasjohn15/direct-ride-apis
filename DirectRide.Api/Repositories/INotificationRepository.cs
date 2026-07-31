using DirectRide.Api.Models;

namespace DirectRide.Api.Repositories;

public interface INotificationRepository
{
    void Add(Notification notification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

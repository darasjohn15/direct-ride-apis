namespace DirectRide.Api.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public NotificationType NotificationType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? RideId { get; set; }
    public RideRequest? Ride { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }
}

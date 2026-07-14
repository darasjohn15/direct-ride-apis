namespace DirectRide.Api.DTOs.Notifications;

public class NotificationResponseDto
{
    public Guid Id { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? RideId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}

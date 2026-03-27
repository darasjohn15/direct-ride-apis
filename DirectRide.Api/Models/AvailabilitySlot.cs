namespace DirectRide.Api.Models;

public class AvailabilitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DriverId { get; set; }

    public User? Driver { get; set; } // navigation property

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsBooked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RideRequest? RideRequest { get; set; }
}
namespace DirectRide.Api.Models;

public class RideRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RiderId { get; set; }
    public User? Rider { get; set; }

    public Guid DriverId { get; set; }
    public User? Driver { get; set; }

    public Guid AvailabilitySlotId { get; set; }
    public AvailabilitySlot? AvailabilitySlot { get; set; }

    public string PickupLocation { get; set; } = string.Empty;

    public string DropoffLocation { get; set; } = string.Empty;

    public RideRequestStatus Status { get; set; } = RideRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
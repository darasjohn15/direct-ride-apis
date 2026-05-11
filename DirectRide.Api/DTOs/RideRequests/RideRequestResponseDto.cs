namespace DirectRide.Api.DTOs.RideRequests;

public class RideRequestResponseDto
{
    public Guid Id { get; set; }

    public Guid RiderId { get; set; }

    public string RiderName { get; set; } = string.Empty;

    public Guid DriverId { get; set; }

    public string DriverName { get; set; } = string.Empty;

    public Guid AvailabilitySlotId { get; set; }

    public DateTime SlotStartTime { get; set; }

    public DateTime SlotEndTime { get; set; }

    public string PickupLocation { get; set; } = string.Empty;

    public string DropoffLocation { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
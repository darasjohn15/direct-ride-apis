namespace DirectRide.Api.DTOs.RideRequests;

public class CreateRideRequestDto
{
    public Guid RiderId { get; set; }

    public Guid DriverId { get; set; }

    public Guid AvailabilitySlotId { get; set; }

    public string PickupLocation { get; set; } = string.Empty;

    public string DropoffLocation { get; set; } = string.Empty;
}
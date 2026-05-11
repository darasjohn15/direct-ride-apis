namespace DirectRide.Api.DTOs.AvailabilitySlots;

public class CreateAvailabilitySlotDto
{
    public Guid DriverId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}
namespace DirectRide.Api.DTOs.AvailabilitySlots;

public class AvailabilitySlotResponseDto
{
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }

    public string DriverName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsBooked { get; set; }

    public DateTime CreatedAt { get; set; }
}
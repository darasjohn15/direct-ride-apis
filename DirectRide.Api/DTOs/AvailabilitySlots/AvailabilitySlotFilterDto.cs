namespace DirectRide.Api.DTOs.AvailabilitySlots;

public class AvailabilitySlotFilterDto
{
    public Guid? DriverId { get; set; }

    public string? DriverName { get; set; }

    public DateTime? StartTimeFrom { get; set; }

    public DateTime? StartTimeTo { get; set; }

    public DateTime? EndTimeFrom { get; set; }

    public DateTime? EndTimeTo { get; set; }

    public bool? IsBooked { get; set; }

    public DateTime? CreatedAtFrom { get; set; }

    public DateTime? CreatedAtTo { get; set; }
}

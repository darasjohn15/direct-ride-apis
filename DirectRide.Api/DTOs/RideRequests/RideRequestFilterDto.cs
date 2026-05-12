using DirectRide.Api.Models;

namespace DirectRide.Api.DTOs.RideRequests;

public class RideRequestFilterDto
{
    public Guid? RiderId { get; set; }

    public string? RiderName { get; set; }

    public Guid? DriverId { get; set; }

    public string? DriverName { get; set; }

    public Guid? AvailabilitySlotId { get; set; }

    public string? PickupLocation { get; set; }

    public string? DropoffLocation { get; set; }

    public RideRequestStatus? Status { get; set; }

    public DateTime? SlotStartTimeFrom { get; set; }

    public DateTime? SlotStartTimeTo { get; set; }

    public DateTime? SlotEndTimeFrom { get; set; }

    public DateTime? SlotEndTimeTo { get; set; }

    public DateTime? CreatedAtFrom { get; set; }

    public DateTime? CreatedAtTo { get; set; }
}

using DirectRide.Api.Models;

namespace DirectRide.Api.DTOs.RideRequests;

public class UpdateRideRequestDto
{
    public Guid RiderId { get; set; }

    public Guid DriverId { get; set; }

    public Guid AvailabilitySlotId { get; set; }

    public string PickupLocation { get; set; } = string.Empty;

    public string DropoffLocation { get; set; } = string.Empty;

    public decimal FareAmount { get; set; }

    public decimal DriverEarningsAmount { get; set; }

    public RideRequestStatus Status { get; set; }

    public DateTime ScheduledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public string? CancellationReason { get; set; }
}

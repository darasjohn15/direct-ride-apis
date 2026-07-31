using DirectRide.Api.Models;

namespace DirectRide.Api.Repositories;

public interface IRideRequestRepository
{
    IQueryable<RideRequest> QueryWithDetails();
    Task<RideRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AvailabilitySlotHasRideRequestAsync(
        Guid excludedRideRequestId,
        Guid availabilitySlotId,
        CancellationToken cancellationToken = default);
    void Add(RideRequest rideRequest);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

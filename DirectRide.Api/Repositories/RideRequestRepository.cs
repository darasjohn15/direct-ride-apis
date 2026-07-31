using DirectRide.Api.Data;
using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Repositories;

public class RideRequestRepository : IRideRequestRepository
{
    private readonly AppDbContext _db;

    public RideRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<RideRequest> QueryWithDetails()
    {
        return _db.RideRequests
            .Include(r => r.Rider)
            .Include(r => r.Driver)
            .Include(r => r.AvailabilitySlot)
            .AsQueryable();
    }

    public async Task<RideRequest?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> AvailabilitySlotHasRideRequestAsync(
        Guid excludedRideRequestId,
        Guid availabilitySlotId,
        CancellationToken cancellationToken = default)
    {
        return await _db.RideRequests
            .AnyAsync(r =>
                r.Id != excludedRideRequestId &&
                r.AvailabilitySlotId == availabilitySlotId,
                cancellationToken);
    }

    public void Add(RideRequest rideRequest)
    {
        _db.RideRequests.Add(rideRequest);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}

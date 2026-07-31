using DirectRide.Api.Data;
using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Repositories;

public class AvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly AppDbContext _db;

    public AvailabilitySlotRepository(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<AvailabilitySlot> QueryWithDriver()
    {
        return _db.AvailabilitySlots
            .Include(a => a.Driver)
            .AsQueryable();
    }

    public async Task<AvailabilitySlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.AvailabilitySlots.FindAsync([id], cancellationToken);
    }

    public async Task<bool> HasOverlappingSlotAsync(
        Guid driverId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        return await _db.AvailabilitySlots.AnyAsync(existing =>
            existing.DriverId == driverId &&
            existing.StartTime < endTime &&
            existing.EndTime > startTime,
            cancellationToken);
    }

    public void AddRange(IEnumerable<AvailabilitySlot> slots)
    {
        _db.AvailabilitySlots.AddRange(slots);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}

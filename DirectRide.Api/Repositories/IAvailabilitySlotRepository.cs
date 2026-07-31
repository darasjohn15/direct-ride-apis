using DirectRide.Api.Models;

namespace DirectRide.Api.Repositories;

public interface IAvailabilitySlotRepository
{
    IQueryable<AvailabilitySlot> QueryWithDriver();
    Task<AvailabilitySlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingSlotAsync(
        Guid driverId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<AvailabilitySlot> slots);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

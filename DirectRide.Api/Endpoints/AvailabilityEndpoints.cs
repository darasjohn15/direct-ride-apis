using DirectRide.Api.Data;
using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class AvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/availability")
            .RequireAuthorization();

        group.MapGet("", async (AppDbContext db, [AsParameters] AvailabilitySlotFilterDto filters) =>
        {
            var query = db.AvailabilitySlots
                .Include(a => a.Driver)
                .AsQueryable();

            if (filters.DriverId.HasValue)
            {
                query = query.Where(a => a.DriverId == filters.DriverId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.DriverName))
            {
                var driverName = $"%{filters.DriverName.Trim().ToLower()}%";
                query = query.Where(a => a.Driver != null &&
                    EF.Functions.Like((a.Driver.FirstName + " " + a.Driver.LastName).ToLower(), driverName));
            }

            if (filters.StartTimeFrom.HasValue)
            {
                query = query.Where(a => a.StartTime >= filters.StartTimeFrom.Value);
            }

            if (filters.StartTimeTo.HasValue)
            {
                query = query.Where(a => a.StartTime <= filters.StartTimeTo.Value);
            }

            if (filters.EndTimeFrom.HasValue)
            {
                query = query.Where(a => a.EndTime >= filters.EndTimeFrom.Value);
            }

            if (filters.EndTimeTo.HasValue)
            {
                query = query.Where(a => a.EndTime <= filters.EndTimeTo.Value);
            }

            query = filters.IsBooked.HasValue
                ? query.Where(a => a.IsBooked == filters.IsBooked.Value)
                : query.Where(a => !a.IsBooked);

            if (filters.CreatedAtFrom.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filters.CreatedAtFrom.Value);
            }

            if (filters.CreatedAtTo.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= filters.CreatedAtTo.Value);
            }

            var slots = await query
                .OrderBy(a => a.StartTime)
                .Select(a => new AvailabilitySlotResponseDto
                {
                    Id = a.Id,
                    DriverId = a.DriverId,
                    DriverName = a.Driver != null
                        ? $"{a.Driver.FirstName} {a.Driver.LastName}"
                        : string.Empty,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsBooked = a.IsBooked,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(slots);
        });

        group.MapPost("", async (AppDbContext db, CreateAvailabilitySlotDto dto) =>
        {
            var driver = await db.Users.FindAsync(dto.DriverId);

            if (driver is null)
            {
                return Results.NotFound("Driver not found.");
            }

            if (driver.Role != UserRole.Driver)
            {
                return Results.BadRequest("User is not a driver.");
            }

            if (dto.StartTime >= dto.EndTime)
            {
                return Results.BadRequest("Start time must be before end time.");
            }

            var slotDuration = TimeSpan.FromHours(1);
            var windowDuration = dto.EndTime - dto.StartTime;

            if (windowDuration < slotDuration)
            {
                return Results.BadRequest("Availability window must be at least one hour.");
            }

            if (windowDuration.Ticks % slotDuration.Ticks != 0)
            {
                return Results.BadRequest("Availability window must divide evenly into one-hour slots.");
            }

            var slots = new List<AvailabilitySlot>();

            for (var startTime = dto.StartTime; startTime < dto.EndTime; startTime = startTime.Add(slotDuration))
            {
                slots.Add(new AvailabilitySlot
                {
                    DriverId = dto.DriverId,
                    StartTime = startTime,
                    EndTime = startTime.Add(slotDuration)
                });
            }

            var hasOverlap = await db.AvailabilitySlots.AnyAsync(existing =>
                existing.DriverId == dto.DriverId &&
                existing.StartTime < dto.EndTime &&
                existing.EndTime > dto.StartTime);

            if (hasOverlap)
            {
                return Results.BadRequest("Availability window overlaps an existing slot for this driver.");
            }

            db.AvailabilitySlots.AddRange(slots);
            await db.SaveChangesAsync();

            var response = slots
                .OrderBy(slot => slot.StartTime)
                .Select(slot => new AvailabilitySlotResponseDto
                {
                    Id = slot.Id,
                    DriverId = slot.DriverId,
                    DriverName = $"{driver.FirstName} {driver.LastName}",
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    IsBooked = slot.IsBooked,
                    CreatedAt = slot.CreatedAt
                })
                .ToList();

            return Results.Created("/availability", response);
        });

        return app;
    }
}

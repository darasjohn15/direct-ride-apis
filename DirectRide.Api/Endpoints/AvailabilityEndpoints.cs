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

            var slot = new AvailabilitySlot
            {
                DriverId = dto.DriverId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            };

            db.AvailabilitySlots.Add(slot);
            await db.SaveChangesAsync();

            var response = new AvailabilitySlotResponseDto
            {
                Id = slot.Id,
                DriverId = slot.DriverId,
                DriverName = $"{driver.FirstName} {driver.LastName}",
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsBooked = slot.IsBooked,
                CreatedAt = slot.CreatedAt
            };

            return Results.Created($"/availability/{slot.Id}", response);
        });

        return app;
    }
}

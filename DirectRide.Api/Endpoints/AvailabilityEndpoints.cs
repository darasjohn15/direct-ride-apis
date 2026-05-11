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

        group.MapGet("", async (AppDbContext db) =>
        {
            var slots = await db.AvailabilitySlots
                .Where(a => !a.IsBooked)
                .Include(a => a.Driver)
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

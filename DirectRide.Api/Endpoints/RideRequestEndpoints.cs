using DirectRide.Api.Data;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class RideRequestEndpoints
{
    public static IEndpointRouteBuilder MapRideRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ride-requests")
            .RequireAuthorization();

        group.MapGet("", async (AppDbContext db) =>
        {
            var rideRequests = await db.RideRequests
                .Include(r => r.Rider)
                .Include(r => r.Driver)
                .Include(r => r.AvailabilitySlot)
                .Select(r => new RideRequestResponseDto
                {
                    Id = r.Id,
                    RiderId = r.RiderId,
                    RiderName = r.Rider != null
                        ? $"{r.Rider.FirstName} {r.Rider.LastName}"
                        : string.Empty,
                    DriverId = r.DriverId,
                    DriverName = r.Driver != null
                        ? $"{r.Driver.FirstName} {r.Driver.LastName}"
                        : string.Empty,
                    AvailabilitySlotId = r.AvailabilitySlotId,
                    SlotStartTime = r.AvailabilitySlot != null ? r.AvailabilitySlot.StartTime : default,
                    SlotEndTime = r.AvailabilitySlot != null ? r.AvailabilitySlot.EndTime : default,
                    PickupLocation = r.PickupLocation,
                    DropoffLocation = r.DropoffLocation,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(rideRequests);
        });

        group.MapPost("", async (AppDbContext db, CreateRideRequestDto dto) =>
        {
            var slot = await db.AvailabilitySlots.FindAsync(dto.AvailabilitySlotId);

            if (slot is null)
            {
                return Results.NotFound("Availability slot not found.");
            }

            if (slot.IsBooked)
            {
                return Results.BadRequest("That availability slot is already booked.");
            }

            var rider = await db.Users.FindAsync(dto.RiderId);
            if (rider is null)
            {
                return Results.NotFound("Rider not found.");
            }

            var driver = await db.Users.FindAsync(dto.DriverId);
            if (driver is null)
            {
                return Results.NotFound("Driver not found.");
            }

            var request = new RideRequest
            {
                RiderId = dto.RiderId,
                DriverId = dto.DriverId,
                AvailabilitySlotId = dto.AvailabilitySlotId,
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation
            };

            db.RideRequests.Add(request);
            slot.IsBooked = true;

            await db.SaveChangesAsync();

            var response = new RideRequestResponseDto
            {
                Id = request.Id,
                RiderId = request.RiderId,
                RiderName = $"{rider.FirstName} {rider.LastName}",
                DriverId = request.DriverId,
                DriverName = $"{driver.FirstName} {driver.LastName}",
                AvailabilitySlotId = request.AvailabilitySlotId,
                SlotStartTime = slot.StartTime,
                SlotEndTime = slot.EndTime,
                PickupLocation = request.PickupLocation,
                DropoffLocation = request.DropoffLocation,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt
            };

            return Results.Created($"/ride-requests/{request.Id}", response);
        });

        group.MapPatch("/{id}/status", async (AppDbContext db, Guid id, RideRequestStatus status) =>
        {
            var request = await db.RideRequests
                .Include(r => r.Rider)
                .Include(r => r.Driver)
                .Include(r => r.AvailabilitySlot)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return Results.NotFound("Ride request not found.");
            }

            request.Status = status;

            if (status == RideRequestStatus.Declined && request.AvailabilitySlot is not null)
            {
                request.AvailabilitySlot.IsBooked = false;
            }

            await db.SaveChangesAsync();

            var response = new RideRequestResponseDto
            {
                Id = request.Id,
                RiderId = request.RiderId,
                RiderName = request.Rider != null
                    ? $"{request.Rider.FirstName} {request.Rider.LastName}"
                    : string.Empty,
                DriverId = request.DriverId,
                DriverName = request.Driver != null
                    ? $"{request.Driver.FirstName} {request.Driver.LastName}"
                    : string.Empty,
                AvailabilitySlotId = request.AvailabilitySlotId,
                SlotStartTime = request.AvailabilitySlot?.StartTime ?? default,
                SlotEndTime = request.AvailabilitySlot?.EndTime ?? default,
                PickupLocation = request.PickupLocation,
                DropoffLocation = request.DropoffLocation,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt
            };

            return Results.Ok(response);
        });

        return app;
    }
}

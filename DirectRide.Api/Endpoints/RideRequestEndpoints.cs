using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.Data;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Models;
using DirectRide.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class RideRequestEndpoints
{
    public static IEndpointRouteBuilder MapRideRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ride-requests")
            .RequireAuthorization();

        group.MapGet("", async (AppDbContext db, [AsParameters] RideRequestFilterDto filters) =>
        {
            var page = Math.Max(filters.Page ?? 1, 1);
            var pageSize = Math.Clamp(filters.PageSize ?? 20, 1, 100);

            var query = db.RideRequests
                .Include(r => r.Rider)
                .Include(r => r.Driver)
                .Include(r => r.AvailabilitySlot)
                .AsQueryable();

            if (filters.RiderId.HasValue)
            {
                query = query.Where(r => r.RiderId == filters.RiderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.RiderName))
            {
                var riderName = $"%{filters.RiderName.Trim().ToLower()}%";
                query = query.Where(r => r.Rider != null &&
                    EF.Functions.Like((r.Rider.FirstName + " " + r.Rider.LastName).ToLower(), riderName));
            }

            if (filters.DriverId.HasValue)
            {
                query = query.Where(r => r.DriverId == filters.DriverId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.DriverName))
            {
                var driverName = $"%{filters.DriverName.Trim().ToLower()}%";
                query = query.Where(r => r.Driver != null &&
                    EF.Functions.Like((r.Driver.FirstName + " " + r.Driver.LastName).ToLower(), driverName));
            }

            if (filters.AvailabilitySlotId.HasValue)
            {
                query = query.Where(r => r.AvailabilitySlotId == filters.AvailabilitySlotId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.PickupLocation))
            {
                var pickupLocation = $"%{filters.PickupLocation.Trim().ToLower()}%";
                query = query.Where(r => EF.Functions.Like(r.PickupLocation.ToLower(), pickupLocation));
            }

            if (!string.IsNullOrWhiteSpace(filters.DropoffLocation))
            {
                var dropoffLocation = $"%{filters.DropoffLocation.Trim().ToLower()}%";
                query = query.Where(r => EF.Functions.Like(r.DropoffLocation.ToLower(), dropoffLocation));
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(r => r.Status == filters.Status.Value);
            }

            if (filters.UpcomingOnly == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(r => r.AvailabilitySlot != null &&
                    r.AvailabilitySlot.StartTime > now);
            }

            if (filters.SlotStartTimeFrom.HasValue)
            {
                query = query.Where(r => r.AvailabilitySlot != null &&
                    r.AvailabilitySlot.StartTime >= filters.SlotStartTimeFrom.Value);
            }

            if (filters.SlotStartTimeTo.HasValue)
            {
                query = query.Where(r => r.AvailabilitySlot != null &&
                    r.AvailabilitySlot.StartTime <= filters.SlotStartTimeTo.Value);
            }

            if (filters.SlotEndTimeFrom.HasValue)
            {
                query = query.Where(r => r.AvailabilitySlot != null &&
                    r.AvailabilitySlot.EndTime >= filters.SlotEndTimeFrom.Value);
            }

            if (filters.SlotEndTimeTo.HasValue)
            {
                query = query.Where(r => r.AvailabilitySlot != null &&
                    r.AvailabilitySlot.EndTime <= filters.SlotEndTimeTo.Value);
            }

            if (filters.CreatedAtFrom.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filters.CreatedAtFrom.Value);
            }

            if (filters.CreatedAtTo.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= filters.CreatedAtTo.Value);
            }

            var orderedQuery = filters.UpcomingOnly == true
                ? query.OrderBy(r => r.AvailabilitySlot!.StartTime)
                    .ThenByDescending(r => r.CreatedAt)
                    .ThenBy(r => r.Id)
                : query.OrderByDescending(r => r.CreatedAt)
                    .ThenBy(r => r.Id);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var effectivePage = totalPages > 0 ? Math.Min(page, totalPages) : 1;

            var rideRequests = await orderedQuery
                .Skip((effectivePage - 1) * pageSize)
                .Take(pageSize)
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
                    FareAmount = r.FareAmount,
                    DriverEarningsAmount = r.DriverEarningsAmount,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    CompletedAt = r.CompletedAt
                })
                .ToListAsync();

            return Results.Ok(new PaginatedResponseDto<RideRequestResponseDto>
            {
                Items = rideRequests,
                Page = effectivePage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPreviousPage = effectivePage > 1,
                HasNextPage = effectivePage < totalPages
            });
        });

        group.MapGet("/{id:guid}", async (AppDbContext db, Guid id) =>
        {
            var rideRequest = await db.RideRequests
                .Include(r => r.Rider)
                .Include(r => r.Driver)
                .Include(r => r.AvailabilitySlot)
                .Where(r => r.Id == id)
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
                    FareAmount = r.FareAmount,
                    DriverEarningsAmount = r.DriverEarningsAmount,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    CompletedAt = r.CompletedAt
                })
                .FirstOrDefaultAsync();

            return rideRequest is null
                ? Results.NotFound("Ride request not found.")
                : Results.Ok(rideRequest);
        });

        group.MapPost("", async (
            AppDbContext db,
            NotificationService notificationService,
            CreateRideRequestDto dto) =>
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

            var driver = await db.Users.FindAsync(slot.DriverId);
            if (driver is null)
            {
                return Results.NotFound("Driver not found.");
            }

            var request = new RideRequest
            {
                RiderId = dto.RiderId,
                DriverId = slot.DriverId,
                AvailabilitySlotId = dto.AvailabilitySlotId,
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation,
                FareAmount = driver.BaseFare,
                DriverEarningsAmount = driver.BaseFare
            };

            db.RideRequests.Add(request);
            slot.IsBooked = true;

            await db.SaveChangesAsync();

            await notificationService.CreateNotificationAsync(
                request.DriverId,
                NotificationType.RideRequested,
                "New ride request",
                "You have a new ride request.",
                request.Id);

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
                FareAmount = request.FareAmount,
                DriverEarningsAmount = request.DriverEarningsAmount,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                CompletedAt = request.CompletedAt
            };

            return Results.Created($"/ride-requests/{request.Id}", response);
        });

        group.MapPut("/{id:guid}", async (
            ClaimsPrincipal claimsPrincipal,
            AppDbContext db,
            NotificationService notificationService,
            Guid id,
            UpdateRideRequestDto dto) =>
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

            if (driver.Role != UserRole.Driver)
            {
                return Results.BadRequest("User is not a driver.");
            }

            var slot = await db.AvailabilitySlots.FindAsync(dto.AvailabilitySlotId);
            if (slot is null)
            {
                return Results.NotFound("Availability slot not found.");
            }

            if (slot.DriverId != dto.DriverId)
            {
                return Results.BadRequest("Availability slot does not belong to the selected driver.");
            }

            var slotIsBookedByAnotherRide = await db.RideRequests
                .AnyAsync(r => r.Id != id && r.AvailabilitySlotId == dto.AvailabilitySlotId);

            if (slotIsBookedByAnotherRide)
            {
                return Results.BadRequest("That availability slot is already booked.");
            }

            if (request.AvailabilitySlotId != dto.AvailabilitySlotId && request.AvailabilitySlot is not null)
            {
                request.AvailabilitySlot.IsBooked = false;
            }

            var previousStatus = request.Status;

            request.RiderId = dto.RiderId;
            request.DriverId = dto.DriverId;
            request.AvailabilitySlotId = dto.AvailabilitySlotId;
            request.PickupLocation = dto.PickupLocation;
            request.DropoffLocation = dto.DropoffLocation;
            request.FareAmount = dto.FareAmount;
            request.DriverEarningsAmount = dto.DriverEarningsAmount;
            request.Status = dto.Status;
            request.CreatedAt = dto.CreatedAt;
            request.CompletedAt = dto.CompletedAt;

            slot.IsBooked = dto.Status != RideRequestStatus.Declined;

            await db.SaveChangesAsync();

            TryGetUserId(claimsPrincipal, out var actorUserId);
            await CreateRideStatusNotificationAsync(
                notificationService,
                previousStatus,
                request.Status,
                request,
                actorUserId);

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
                FareAmount = request.FareAmount,
                DriverEarningsAmount = request.DriverEarningsAmount,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                CompletedAt = request.CompletedAt
            };

            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPatch("/{id}/status", async (
            ClaimsPrincipal claimsPrincipal,
            AppDbContext db,
            NotificationService notificationService,
            Guid id,
            RideRequestStatus status) =>
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

            var previousStatus = request.Status;
            request.Status = status;

            if (status == RideRequestStatus.Declined && request.AvailabilitySlot is not null)
            {
                request.AvailabilitySlot.IsBooked = false;
            }

            if (status == RideRequestStatus.Completed)
            {
                request.CompletedAt ??= DateTime.UtcNow;

                if (request.FareAmount == 0.00m && request.Driver is not null)
                {
                    request.FareAmount = request.Driver.BaseFare;
                    request.DriverEarningsAmount = request.Driver.BaseFare;
                }
            }
            else
            {
                request.CompletedAt = null;
            }

            await db.SaveChangesAsync();

            TryGetUserId(claimsPrincipal, out var actorUserId);
            await CreateRideStatusNotificationAsync(
                notificationService,
                previousStatus,
                request.Status,
                request,
                actorUserId);

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
                FareAmount = request.FareAmount,
                DriverEarningsAmount = request.DriverEarningsAmount,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                CompletedAt = request.CompletedAt
            };

            return Results.Ok(response);
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal claimsPrincipal, out Guid userId)
    {
        var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdClaim, out userId);
    }

    private static async Task CreateRideStatusNotificationAsync(
        NotificationService notificationService,
        RideRequestStatus previousStatus,
        RideRequestStatus currentStatus,
        RideRequest request,
        Guid actorUserId)
    {
        if (previousStatus == currentStatus)
        {
            return;
        }

        switch (currentStatus)
        {
            case RideRequestStatus.Accepted:
                await notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideAccepted,
                    "Ride request accepted",
                    "Your ride request was accepted.",
                    request.Id);
                break;

            case RideRequestStatus.Declined:
                await notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideDenied,
                    "Ride request declined",
                    "Your ride request was declined.",
                    request.Id);
                break;

            case RideRequestStatus.Cancelled when actorUserId == request.RiderId:
                await notificationService.CreateNotificationAsync(
                    request.DriverId,
                    NotificationType.RideCancelled,
                    "Ride canceled",
                    "The rider canceled the ride.",
                    request.Id);
                break;

            case RideRequestStatus.Cancelled when actorUserId == request.DriverId:
                await notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideCancelled,
                    "Ride canceled",
                    "Your driver canceled the ride.",
                    request.Id);
                break;
        }
    }
}

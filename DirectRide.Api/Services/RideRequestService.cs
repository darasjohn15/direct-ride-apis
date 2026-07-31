using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Models;
using DirectRide.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Services;

public class RideRequestService
{
    private readonly IAvailabilitySlotRepository _availabilitySlots;
    private readonly NotificationService _notificationService;
    private readonly IRideRequestRepository _rideRequests;
    private readonly IUserRepository _users;

    public RideRequestService(
        IRideRequestRepository rideRequests,
        IAvailabilitySlotRepository availabilitySlots,
        IUserRepository users,
        NotificationService notificationService)
    {
        _rideRequests = rideRequests;
        _availabilitySlots = availabilitySlots;
        _users = users;
        _notificationService = notificationService;
    }

    public async Task<PaginatedResponseDto<RideRequestResponseDto>> GetRideRequestsAsync(
        RideRequestFilterDto filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page ?? 1, 1);
        var pageSize = Math.Clamp(filters.PageSize ?? 20, 1, 100);

        var query = _rideRequests.QueryWithDetails();

        query = ApplyFilters(query, filters);

        var orderedQuery = filters.UpcomingOnly == true
            ? query.OrderBy(r => r.AvailabilitySlot!.StartTime)
                .ThenByDescending(r => r.CreatedAt)
                .ThenBy(r => r.Id)
            : query.OrderByDescending(r => r.CreatedAt)
                .ThenBy(r => r.Id);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var effectivePage = totalPages > 0 ? Math.Min(page, totalPages) : 1;

        var rideRequests = await orderedQuery
            .Skip((effectivePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponseDto<RideRequestResponseDto>
        {
            Items = rideRequests.Select(ToResponseDto).ToList(),
            Page = effectivePage,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = effectivePage > 1,
            HasNextPage = effectivePage < totalPages
        };
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> GetRideRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var rideRequest = await _rideRequests.GetByIdWithDetailsAsync(id, cancellationToken);

        return rideRequest is null
            ? RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.")
            : RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(rideRequest));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> CreateRideRequestAsync(
        CreateRideRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var slot = await _availabilitySlots.GetByIdAsync(dto.AvailabilitySlotId, cancellationToken);

        if (slot is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Availability slot not found.");
        }

        if (slot.IsBooked)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest("That availability slot is already booked.");
        }

        var rider = await _users.GetByIdAsync(dto.RiderId, cancellationToken);
        if (rider is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Rider not found.");
        }

        var driver = await _users.GetByIdAsync(slot.DriverId, cancellationToken);
        if (driver is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Driver not found.");
        }

        var request = new RideRequest
        {
            RiderId = dto.RiderId,
            DriverId = slot.DriverId,
            AvailabilitySlotId = dto.AvailabilitySlotId,
            PickupLocation = dto.PickupLocation,
            DropoffLocation = dto.DropoffLocation,
            ScheduledAt = dto.ScheduledAt ?? slot.StartTime,
            FareAmount = driver.BaseFare,
            DriverEarningsAmount = driver.BaseFare
        };

        _rideRequests.Add(request);
        slot.IsBooked = true;

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationAsync(
            request.DriverId,
            NotificationType.RideRequested,
            "New ride request",
            "You have a new ride request.",
            request.Id,
            cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Created(ToResponseDto(request, rider, driver, slot));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> UpdateRideRequestAsync(
        Guid id,
        UpdateRideRequestDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRideRequestEntityAsync(id, cancellationToken);

        if (request is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.");
        }

        var rider = await _users.GetByIdAsync(dto.RiderId, cancellationToken);
        if (rider is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Rider not found.");
        }

        var driver = await _users.GetByIdAsync(dto.DriverId, cancellationToken);
        if (driver is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Driver not found.");
        }

        if (driver.Role != UserRole.Driver)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest("User is not a driver.");
        }

        var slot = await _availabilitySlots.GetByIdAsync(dto.AvailabilitySlotId, cancellationToken);
        if (slot is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Availability slot not found.");
        }

        if (slot.DriverId != dto.DriverId)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest(
                "Availability slot does not belong to the selected driver.");
        }

        var slotIsBookedByAnotherRide = await _rideRequests.AvailabilitySlotHasRideRequestAsync(
            id,
            dto.AvailabilitySlotId,
            cancellationToken);

        if (slotIsBookedByAnotherRide)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest("That availability slot is already booked.");
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
        request.ScheduledAt = dto.ScheduledAt == default ? slot.StartTime : dto.ScheduledAt;
        request.CreatedAt = dto.CreatedAt;
        request.StartedAt = dto.StartedAt;
        request.CompletedAt = dto.CompletedAt;
        request.CancelledAt = dto.CancelledAt;
        request.CancelledByUserId = dto.CancelledByUserId;
        request.CancellationReason = dto.CancellationReason;

        slot.IsBooked = dto.Status != RideRequestStatus.Declined;

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await CreateRideStatusNotificationAsync(previousStatus, request.Status, request, actorUserId, cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(request, rider, driver, slot));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> StartRideRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRideRequestEntityAsync(id, cancellationToken);

        if (request is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.");
        }

        if (request.Status != RideRequestStatus.Accepted)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest(
                "Ride must be accepted before it can be started.");
        }

        request.Status = RideRequestStatus.InProgress;
        request.StartedAt ??= DateTime.UtcNow;
        request.CompletedAt = null;
        request.CancelledAt = null;
        request.CancelledByUserId = null;
        request.CancellationReason = null;

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationAsync(
            request.RiderId,
            NotificationType.RideStarted,
            "Ride started",
            "Your ride has started.",
            request.Id,
            cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(request));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> CompleteRideRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRideRequestEntityAsync(id, cancellationToken);

        if (request is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.");
        }

        if (request.Status != RideRequestStatus.InProgress)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest(
                "Ride must be in progress before it can be completed.");
        }

        request.Status = RideRequestStatus.Completed;
        request.CompletedAt ??= DateTime.UtcNow;

        SetBaseFareIfMissing(request);

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationAsync(
            request.RiderId,
            NotificationType.RideCompleted,
            "Ride completed",
            "Your ride has been completed.",
            request.Id,
            cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(request));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> CancelRideRequestAsync(
        Guid id,
        Guid actorUserId,
        string? cancellationReason,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRideRequestEntityAsync(id, cancellationToken);

        if (request is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.");
        }

        if (request.Status != RideRequestStatus.Accepted)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.BadRequest(
                "Ride must be accepted before it can be cancelled.");
        }

        request.Status = RideRequestStatus.Cancelled;
        request.CancelledAt ??= DateTime.UtcNow;
        request.CancelledByUserId = actorUserId == default ? null : actorUserId;
        request.CancellationReason = string.IsNullOrWhiteSpace(cancellationReason)
            ? null
            : cancellationReason.Trim();

        if (request.AvailabilitySlot is not null)
        {
            request.AvailabilitySlot.IsBooked = false;
        }

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await CreateRideCancelledNotificationAsync(request, actorUserId, cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(request));
    }

    public async Task<RideRequestServiceResult<RideRequestResponseDto>> UpdateRideRequestStatusAsync(
        Guid id,
        RideRequestStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRideRequestEntityAsync(id, cancellationToken);

        if (request is null)
        {
            return RideRequestServiceResult<RideRequestResponseDto>.NotFound("Ride request not found.");
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
            SetBaseFareIfMissing(request);
        }
        else
        {
            request.CompletedAt = null;
        }

        if (status == RideRequestStatus.InProgress)
        {
            request.StartedAt ??= DateTime.UtcNow;
        }
        else if (status != RideRequestStatus.Completed)
        {
            request.StartedAt = null;
        }

        if (status == RideRequestStatus.Cancelled)
        {
            request.CancelledAt ??= DateTime.UtcNow;
            request.CancelledByUserId ??= actorUserId == default ? null : actorUserId;
        }
        else
        {
            request.CancelledAt = null;
            request.CancelledByUserId = null;
            request.CancellationReason = null;
        }

        await _rideRequests.SaveChangesAsync(cancellationToken);

        await CreateRideStatusNotificationAsync(previousStatus, request.Status, request, actorUserId, cancellationToken);

        return RideRequestServiceResult<RideRequestResponseDto>.Ok(ToResponseDto(request));
    }

    private IQueryable<RideRequest> ApplyFilters(IQueryable<RideRequest> query, RideRequestFilterDto filters)
    {
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

        return query;
    }

    private async Task<RideRequest?> GetRideRequestEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _rideRequests.GetByIdWithDetailsAsync(id, cancellationToken);
    }

    private static void SetBaseFareIfMissing(RideRequest request)
    {
        if (request.FareAmount == 0.00m && request.Driver is not null)
        {
            request.FareAmount = request.Driver.BaseFare;
            request.DriverEarningsAmount = request.Driver.BaseFare;
        }
    }

    private async Task CreateRideStatusNotificationAsync(
        RideRequestStatus previousStatus,
        RideRequestStatus currentStatus,
        RideRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (previousStatus == currentStatus)
        {
            return;
        }

        switch (currentStatus)
        {
            case RideRequestStatus.Accepted:
                await _notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideAccepted,
                    "Ride request accepted",
                    "Your ride request was accepted.",
                    request.Id,
                    cancellationToken);
                break;

            case RideRequestStatus.Declined:
                await _notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideDenied,
                    "Ride request declined",
                    "Your ride request was declined.",
                    request.Id,
                    cancellationToken);
                break;

            case RideRequestStatus.InProgress:
                await _notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideStarted,
                    "Ride started",
                    "Your ride has started.",
                    request.Id,
                    cancellationToken);
                break;

            case RideRequestStatus.Completed:
                await _notificationService.CreateNotificationAsync(
                    request.RiderId,
                    NotificationType.RideCompleted,
                    "Ride completed",
                    "Your ride has been completed.",
                    request.Id,
                    cancellationToken);
                break;

            case RideRequestStatus.Cancelled:
                await CreateRideCancelledNotificationAsync(request, actorUserId, cancellationToken);
                break;
        }
    }

    private async Task CreateRideCancelledNotificationAsync(
        RideRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == request.RiderId)
        {
            await _notificationService.CreateNotificationAsync(
                request.DriverId,
                NotificationType.RideCancelled,
                "Ride canceled",
                "The rider canceled the ride.",
                request.Id,
                cancellationToken);

            return;
        }

        var message = actorUserId == request.DriverId
            ? "Your driver canceled the ride."
            : "Your ride was canceled.";

        await _notificationService.CreateNotificationAsync(
            request.RiderId,
            NotificationType.RideCancelled,
            "Ride canceled",
            message,
            request.Id,
            cancellationToken);
    }

    private static RideRequestResponseDto ToResponseDto(RideRequest request)
    {
        return new RideRequestResponseDto
        {
            Id = request.Id,
            RiderId = request.RiderId,
            RiderName = request.Rider is not null
                ? $"{request.Rider.FirstName} {request.Rider.LastName}"
                : string.Empty,
            DriverId = request.DriverId,
            DriverName = request.Driver is not null
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
            ScheduledAt = request.ScheduledAt,
            CreatedAt = request.CreatedAt,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
            CancelledAt = request.CancelledAt,
            CancelledByUserId = request.CancelledByUserId,
            CancellationReason = request.CancellationReason
        };
    }

    private static RideRequestResponseDto ToResponseDto(
        RideRequest request,
        User rider,
        User driver,
        AvailabilitySlot slot)
    {
        return new RideRequestResponseDto
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
            ScheduledAt = request.ScheduledAt,
            CreatedAt = request.CreatedAt,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
            CancelledAt = request.CancelledAt,
            CancelledByUserId = request.CancelledByUserId,
            CancellationReason = request.CancellationReason
        };
    }
}

public sealed record RideRequestServiceResult<T>(
    RideRequestServiceResultStatus Status,
    T? Value,
    string? Error)
{
    public static RideRequestServiceResult<T> Ok(T value) =>
        new(RideRequestServiceResultStatus.Ok, value, null);

    public static RideRequestServiceResult<T> Created(T value) =>
        new(RideRequestServiceResultStatus.Created, value, null);

    public static RideRequestServiceResult<T> BadRequest(string error) =>
        new(RideRequestServiceResultStatus.BadRequest, default, error);

    public static RideRequestServiceResult<T> NotFound(string error) =>
        new(RideRequestServiceResultStatus.NotFound, default, error);
}

public enum RideRequestServiceResultStatus
{
    Ok,
    Created,
    BadRequest,
    NotFound
}

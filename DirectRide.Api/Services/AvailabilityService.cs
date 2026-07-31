using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.Models;
using DirectRide.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Services;

public class AvailabilityService
{
    private static readonly TimeSpan SlotDuration = TimeSpan.FromHours(1);
    private readonly IAvailabilitySlotRepository _availabilitySlots;
    private readonly IUserRepository _users;

    public AvailabilityService(IAvailabilitySlotRepository availabilitySlots, IUserRepository users)
    {
        _availabilitySlots = availabilitySlots;
        _users = users;
    }

    public async Task<List<AvailabilitySlotResponseDto>> GetAvailabilitySlotsAsync(
        AvailabilitySlotFilterDto filters,
        CancellationToken cancellationToken = default)
    {
        var query = _availabilitySlots.QueryWithDriver();

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

        return await query
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
            .ToListAsync(cancellationToken);
    }

    public async Task<AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>> CreateAvailabilitySlotsAsync(
        CreateAvailabilitySlotDto dto,
        CancellationToken cancellationToken = default)
    {
        var driver = await _users.GetByIdAsync(dto.DriverId, cancellationToken);

        if (driver is null)
        {
            return AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>.NotFound("Driver not found.");
        }

        if (driver.Role != UserRole.Driver)
        {
            return AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>.BadRequest("User is not a driver.");
        }

        var validationError = ValidateAvailabilityWindow(dto);

        if (validationError is not null)
        {
            return AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>.BadRequest(validationError);
        }

        var hasOverlap = await _availabilitySlots.HasOverlappingSlotAsync(
            dto.DriverId,
            dto.StartTime,
            dto.EndTime,
            cancellationToken);

        if (hasOverlap)
        {
            return AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>.BadRequest(
                "Availability window overlaps an existing slot for this driver.");
        }

        var slots = CreateHourlySlots(dto);

        _availabilitySlots.AddRange(slots);
        await _availabilitySlots.SaveChangesAsync(cancellationToken);

        var response = slots
            .OrderBy(slot => slot.StartTime)
            .Select(slot => ToResponse(slot, driver))
            .ToList();

        return AvailabilityServiceResult<List<AvailabilitySlotResponseDto>>.Created(response);
    }

    private static string? ValidateAvailabilityWindow(CreateAvailabilitySlotDto dto)
    {
        if (dto.StartTime >= dto.EndTime)
        {
            return "Start time must be before end time.";
        }

        var windowDuration = dto.EndTime - dto.StartTime;

        if (windowDuration < SlotDuration)
        {
            return "Availability window must be at least one hour.";
        }

        if (windowDuration.Ticks % SlotDuration.Ticks != 0)
        {
            return "Availability window must divide evenly into one-hour slots.";
        }

        return null;
    }

    private static List<AvailabilitySlot> CreateHourlySlots(CreateAvailabilitySlotDto dto)
    {
        var slots = new List<AvailabilitySlot>();

        for (var startTime = dto.StartTime; startTime < dto.EndTime; startTime = startTime.Add(SlotDuration))
        {
            slots.Add(new AvailabilitySlot
            {
                DriverId = dto.DriverId,
                StartTime = startTime,
                EndTime = startTime.Add(SlotDuration)
            });
        }

        return slots;
    }

    private static AvailabilitySlotResponseDto ToResponse(AvailabilitySlot slot, User driver)
    {
        return new AvailabilitySlotResponseDto
        {
            Id = slot.Id,
            DriverId = slot.DriverId,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            IsBooked = slot.IsBooked,
            CreatedAt = slot.CreatedAt
        };
    }
}

public sealed record AvailabilityServiceResult<T>(
    AvailabilityServiceResultStatus Status,
    T? Value,
    string? Error)
{
    public static AvailabilityServiceResult<T> Created(T value) =>
        new(AvailabilityServiceResultStatus.Created, value, null);

    public static AvailabilityServiceResult<T> BadRequest(string error) =>
        new(AvailabilityServiceResultStatus.BadRequest, default, error);

    public static AvailabilityServiceResult<T> NotFound(string error) =>
        new(AvailabilityServiceResultStatus.NotFound, default, error);
}

public enum AvailabilityServiceResultStatus
{
    Created,
    BadRequest,
    NotFound
}

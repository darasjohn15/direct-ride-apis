using DirectRide.Api.Models;
using DirectRide.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using DirectRide.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.MapGet("/users/test", () =>
{
    var user = new User
    {
        FirstName = "Razzo",
        LastName = "Driver",
        Email = "razzo@directride.com",
        PhoneNumber = "555-555-5555",
        Role = UserRole.Driver
    };

    return user;
});

app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Select(u => new UserResponseDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(users);
});

app.MapPost("/users", async (AppDbContext db, CreateUserDto dto) =>
{
    var user = new User
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        PhoneNumber = dto.PhoneNumber,
        Role = (UserRole)dto.Role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var response = new UserResponseDto
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };

    return Results.Created($"/users/{user.Id}", response);
});

app.MapGet("/availability", async (AppDbContext db) =>
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

app.MapPost("/availability", async (AppDbContext db, CreateAvailabilitySlotDto dto) =>
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

app.MapGet("/ride-requests", async (AppDbContext db) =>
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

app.MapPost("/ride-requests", async (AppDbContext db, CreateRideRequestDto dto) =>
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

app.MapPatch("/ride-requests/{id}/status", async (AppDbContext db, Guid id, RideRequestStatus status) =>
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

app.Run();

public partial class Program { }


using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.DTOs.RideRequests;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class AvailabilityApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AvailabilityApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAvailability_ShouldCreateSlot()
    {
        var createDriverResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Driver",
            LastName = "One",
            Email = "driver1@test.com",
            PhoneNumber = "555-222-3333",
            Role = 1
        });

        var driver = await createDriverResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var request = new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var slot = await response.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        slot.Should().NotBeNull();
        slot!.DriverId.Should().Be(driver.Id);
        slot.IsBooked.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailability_ShouldReturnUnbookedSlots()
    {
        var createDriverResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Available",
            LastName = "Driver",
            Email = "available-driver@test.com",
            PhoneNumber = "555-444-5555",
            Role = 1,
            Password = "CorrectHorse123!"
        });
        var driver = await createDriverResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var createSlotResponse = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(1)
        });
        var createdSlot = await createSlotResponse.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        var response = await _client.GetAsync("/availability");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        slots.Should().NotBeNull();
        slots.Should().Contain(s =>
            s.Id == createdSlot!.Id
            && s.DriverId == driver.Id
            && s.DriverName == "Available Driver"
            && !s.IsBooked);
    }

    [Fact]
    public async Task GetAvailability_ShouldFilterByDriverId()
    {
        var matchingDriver = await CreateDriverAsync("filter-driver-id-match@test.com", "Filter", "Match");
        var otherDriver = await CreateDriverAsync("filter-driver-id-other@test.com", "Filter", "Other");
        var matchingSlot = await CreateAvailabilitySlotAsync(matchingDriver.Id, DateTime.UtcNow.AddDays(10));
        var otherSlot = await CreateAvailabilitySlotAsync(otherDriver.Id, DateTime.UtcNow.AddDays(11));

        var response = await _client.GetAsync($"/availability?driverId={matchingDriver.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        slots.Should().NotBeNull();
        slots.Should().Contain(s => s.Id == matchingSlot.Id);
        slots.Should().NotContain(s => s.Id == otherSlot.Id);
    }

    [Fact]
    public async Task GetAvailability_ShouldFilterByDriverName()
    {
        var matchingDriver = await CreateDriverAsync("filter-driver-name-match@test.com", "Serena", "Stone");
        var otherDriver = await CreateDriverAsync("filter-driver-name-other@test.com", "Marcus", "Reed");
        var matchingSlot = await CreateAvailabilitySlotAsync(matchingDriver.Id, DateTime.UtcNow.AddDays(12));
        var otherSlot = await CreateAvailabilitySlotAsync(otherDriver.Id, DateTime.UtcNow.AddDays(13));

        var response = await _client.GetAsync("/availability?driverName=serena");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        slots.Should().NotBeNull();
        slots.Should().Contain(s => s.Id == matchingSlot.Id);
        slots.Should().NotContain(s => s.Id == otherSlot.Id);
    }

    [Fact]
    public async Task GetAvailability_ShouldFilterByStartAndEndTimeRanges()
    {
        var driver = await CreateDriverAsync("filter-time-range-driver@test.com", "Time", "Window");
        var matchingStartTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var outsideStartTime = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);
        var matchingSlot = await CreateAvailabilitySlotAsync(driver.Id, matchingStartTime);
        var outsideSlot = await CreateAvailabilitySlotAsync(driver.Id, outsideStartTime);

        var response = await _client.GetAsync(
            "/availability?startTimeFrom=2026-06-15T00:00:00Z&startTimeTo=2026-06-15T23:59:59Z&endTimeFrom=2026-06-15T09:30:00Z&endTimeTo=2026-06-15T10:30:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        slots.Should().NotBeNull();
        slots.Should().Contain(s => s.Id == matchingSlot.Id);
        slots.Should().NotContain(s => s.Id == outsideSlot.Id);
    }

    [Fact]
    public async Task GetAvailability_ShouldReturnBookedSlots_WhenIsBookedFilterIsTrue()
    {
        var driver = await CreateDriverAsync("filter-booked-driver@test.com", "Booked", "Driver");
        var rider = await CreateRiderAsync("filter-booked-rider@test.com");
        var bookedSlot = await CreateAvailabilitySlotAsync(driver.Id, DateTime.UtcNow.AddDays(14));
        var unbookedSlot = await CreateAvailabilitySlotAsync(driver.Id, DateTime.UtcNow.AddDays(15));

        var rideRequestResponse = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = bookedSlot.Id,
            PickupLocation = "Airport",
            DropoffLocation = "Downtown"
        });

        rideRequestResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync("/availability?isBooked=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        slots.Should().NotBeNull();
        slots.Should().Contain(s => s.Id == bookedSlot.Id && s.IsBooked);
        slots.Should().NotContain(s => s.Id == unbookedSlot.Id);
    }

    [Fact]
    public async Task PostAvailability_ShouldReturnNotFound_WhenDriverDoesNotExist()
    {
        var request = new CreateAvailabilitySlotDto
        {
            DriverId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<UserResponseDto> CreateDriverAsync(string email, string firstName, string lastName)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = "555-222-3333",
            Role = 1,
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        return user!;
    }

    private async Task<UserResponseDto> CreateRiderAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Filter",
            LastName = "Rider",
            Email = email,
            PhoneNumber = "555-333-4444",
            Role = 0,
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        return user!;
    }

    private async Task<AvailabilitySlotResponseDto> CreateAvailabilitySlotAsync(Guid driverId, DateTime startTime)
    {
        var response = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driverId,
            StartTime = startTime,
            EndTime = startTime.AddHours(1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var slot = await response.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        return slot!;
    }
}

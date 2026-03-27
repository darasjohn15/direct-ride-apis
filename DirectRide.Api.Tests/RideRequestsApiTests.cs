using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class RideRequestsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RideRequestsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostRideRequest_ShouldCreateRideRequest_AndBookSlot()
    {
        var driverResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Driver",
            LastName = "User",
            Email = "driver@test.com",
            PhoneNumber = "555-000-0001",
            Role = 1
        });
        var driver = await driverResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var riderResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Rider",
            LastName = "User",
            Email = "rider@test.com",
            PhoneNumber = "555-000-0002",
            Role = 0
        });
        var rider = await riderResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var slotResponse = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        });
        var slot = await slotResponse.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        var request = new CreateRideRequestDto
        {
            RiderId = rider!.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = slot!.Id,
            PickupLocation = "Midtown Atlanta",
            DropoffLocation = "Grant Park"
        };

        var response = await _client.PostAsJsonAsync("/ride-requests", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        rideRequest.Should().NotBeNull();
        rideRequest!.RiderId.Should().Be(rider.Id);
        rideRequest.DriverId.Should().Be(driver.Id);
        rideRequest.AvailabilitySlotId.Should().Be(slot.Id);
        rideRequest.Status.Should().Be("Pending");

        var availabilityResponse = await _client.GetAsync("/availability");
        var availableSlots = await availabilityResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        availableSlots.Should().NotContain(s => s.Id == slot.Id);
    }

    [Fact]
    public async Task PostRideRequest_ShouldReturnBadRequest_WhenSlotAlreadyBooked()
    {
        var driverResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Driver2",
            LastName = "User",
            Email = "driver2@test.com",
            PhoneNumber = "555-000-0003",
            Role = 1
        });
        var driver = await driverResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var rider1Response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Rider1",
            LastName = "User",
            Email = "rider1@test.com",
            PhoneNumber = "555-000-0004",
            Role = 0
        });
        var rider1 = await rider1Response.Content.ReadFromJsonAsync<UserResponseDto>();

        var rider2Response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Rider2",
            LastName = "User",
            Email = "rider2@test.com",
            PhoneNumber = "555-000-0005",
            Role = 0
        });
        var rider2 = await rider2Response.Content.ReadFromJsonAsync<UserResponseDto>();

        var slotResponse = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(1)
        });
        var slot = await slotResponse.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider1!.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = slot!.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        var secondResponse = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider2!.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location C",
            DropoffLocation = "Location D"
        });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
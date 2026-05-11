using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Models;
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

    [Fact]
    public async Task GetRideRequests_ShouldReturnCreatedRideRequests()
    {
        var driver = await CreateUserAsync("ride-list-driver@test.com", role: 1);
        var rider = await CreateUserAsync("ride-list-rider@test.com", role: 0);
        var slot = await CreateAvailabilitySlotAsync(driver.Id, daysFromNow: 3);

        var createResponse = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Ponce City Market",
            DropoffLocation = "Mercedes-Benz Stadium"
        });
        var createdRideRequest = await createResponse.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        var response = await _client.GetAsync("/ride-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequests = await response.Content.ReadFromJsonAsync<List<RideRequestResponseDto>>();

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r =>
            r.Id == createdRideRequest!.Id
            && r.RiderName == "Rider User"
            && r.DriverName == "Driver User"
            && r.PickupLocation == "Ponce City Market"
            && r.DropoffLocation == "Mercedes-Benz Stadium");
    }

    [Fact]
    public async Task PostRideRequest_ShouldReturnNotFound_WhenAvailabilitySlotDoesNotExist()
    {
        var driver = await CreateUserAsync("missing-slot-driver@test.com", role: 1);
        var rider = await CreateUserAsync("missing-slot-rider@test.com", role: 0);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = Guid.NewGuid(),
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostRideRequest_ShouldReturnNotFound_WhenRiderDoesNotExist()
    {
        var driver = await CreateUserAsync("missing-rider-driver@test.com", role: 1);
        var slot = await CreateAvailabilitySlotAsync(driver.Id, daysFromNow: 4);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = Guid.NewGuid(),
            DriverId = driver.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostRideRequest_ShouldReturnNotFound_WhenDriverDoesNotExist()
    {
        var slotDriver = await CreateUserAsync("slot-driver@test.com", role: 1);
        var rider = await CreateUserAsync("missing-driver-rider@test.com", role: 0);
        var slot = await CreateAvailabilitySlotAsync(slotDriver.Id, daysFromNow: 5);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            DriverId = Guid.NewGuid(),
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldUpdateStatus()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "status-driver@test.com",
            riderEmail: "status-rider@test.com",
            daysFromNow: 6);

        var response = await _client.PatchAsync(
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Accepted}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        updatedRideRequest.Should().NotBeNull();
        updatedRideRequest!.Id.Should().Be(rideRequest.Id);
        updatedRideRequest.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldReleaseAvailabilitySlot_WhenDeclined()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "decline-driver@test.com",
            riderEmail: "decline-rider@test.com",
            daysFromNow: 7);

        var response = await _client.PatchAsync(
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Declined}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();
        updatedRideRequest!.Status.Should().Be("Declined");

        var availabilityResponse = await _client.GetAsync("/availability");
        var availableSlots = await availabilityResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        availableSlots.Should().Contain(s => s.Id == rideRequest.AvailabilitySlotId);
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldReturnNotFound_WhenRideRequestDoesNotExist()
    {
        var response = await _client.PatchAsync(
            $"/ride-requests/{Guid.NewGuid()}/status?status={RideRequestStatus.Accepted}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<UserResponseDto> CreateUserAsync(string email, int role)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = role == 1 ? "Driver" : "Rider",
            LastName = "User",
            Email = email,
            PhoneNumber = "555-999-0000",
            Role = role,
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        return user!;
    }

    private async Task<AvailabilitySlotResponseDto> CreateAvailabilitySlotAsync(Guid driverId, int daysFromNow)
    {
        var response = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driverId,
            StartTime = DateTime.UtcNow.AddDays(daysFromNow),
            EndTime = DateTime.UtcNow.AddDays(daysFromNow).AddHours(1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var slot = await response.Content.ReadFromJsonAsync<AvailabilitySlotResponseDto>();

        return slot!;
    }

    private async Task<RideRequestResponseDto> CreateRideRequestAsync(
        string driverEmail,
        string riderEmail,
        int daysFromNow)
    {
        var driver = await CreateUserAsync(driverEmail, role: 1);
        var rider = await CreateUserAsync(riderEmail, role: 0);
        var slot = await CreateAvailabilitySlotAsync(driver.Id, daysFromNow);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            DriverId = driver.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        return rideRequest!;
    }
}

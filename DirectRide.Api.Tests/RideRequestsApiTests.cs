using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Data;
using DirectRide.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectRide.Api.Tests;

public class RideRequestsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RideRequestsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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

        var slotStartTime = DateTime.UtcNow.AddDays(1);
        var slotResponse = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = slotStartTime,
            EndTime = slotStartTime.AddHours(1)
        });
        var slots = await slotResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();
        var slot = slots!.Single();

        var request = new CreateRideRequestDto
        {
            RiderId = rider!.Id,
            AvailabilitySlotId = slot.Id,
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

        var slotStartTime = DateTime.UtcNow.AddDays(2);
        var slotResponse = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driver!.Id,
            StartTime = slotStartTime,
            EndTime = slotStartTime.AddHours(1)
        });
        var slots = await slotResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();
        var slot = slots!.Single();

        await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider1!.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        var secondResponse = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider2!.Id,
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
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Ponce City Market",
            DropoffLocation = "Mercedes-Benz Stadium"
        });
        var createdRideRequest = await createResponse.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        var response = await _client.GetAsync("/ride-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r =>
            r.Id == createdRideRequest!.Id
            && r.RiderName == "Rider User"
            && r.DriverName == "Driver User"
            && r.PickupLocation == "Ponce City Market"
            && r.DropoffLocation == "Mercedes-Benz Stadium");
    }

    [Fact]
    public async Task GetRideRequestById_ShouldReturnRideRequestDetails()
    {
        var createdRideRequest = await CreateRideRequestAsync(
            driverEmail: "ride-detail-driver@test.com",
            riderEmail: "ride-detail-rider@test.com",
            daysFromNow: 4,
            pickupLocation: "State Farm Arena",
            dropoffLocation: "Centennial Olympic Park");

        var response = await _client.GetAsync($"/ride-requests/{createdRideRequest.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        rideRequest.Should().NotBeNull();
        rideRequest!.Id.Should().Be(createdRideRequest.Id);
        rideRequest.RiderId.Should().Be(createdRideRequest.RiderId);
        rideRequest.RiderName.Should().Be("Rider User");
        rideRequest.DriverId.Should().Be(createdRideRequest.DriverId);
        rideRequest.DriverName.Should().Be("Driver User");
        rideRequest.AvailabilitySlotId.Should().Be(createdRideRequest.AvailabilitySlotId);
        rideRequest.PickupLocation.Should().Be("State Farm Arena");
        rideRequest.DropoffLocation.Should().Be("Centennial Olympic Park");
        rideRequest.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetRideRequestById_ShouldReturnNotFound_WhenRideRequestDoesNotExist()
    {
        var response = await _client.GetAsync($"/ride-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRideRequests_ShouldFilterByDriverAndRiderIds()
    {
        var matchingRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-id-driver@test.com",
            riderEmail: "filter-id-rider@test.com",
            daysFromNow: 8);
        var otherRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-id-other-driver@test.com",
            riderEmail: "filter-id-other-rider@test.com",
            daysFromNow: 9);

        var response = await _client.GetAsync(
            $"/ride-requests?driverId={matchingRideRequest.DriverId}&riderId={matchingRideRequest.RiderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r => r.Id == matchingRideRequest.Id);
        rideRequests.Should().NotContain(r => r.Id == otherRideRequest.Id);
    }

    [Fact]
    public async Task GetRideRequests_ShouldFilterByNamesStatusAndLocations()
    {
        var matchingRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-combined-driver@test.com",
            riderEmail: "filter-combined-rider@test.com",
            daysFromNow: 10,
            driverFirstName: "Serena",
            driverLastName: "Stone",
            riderFirstName: "Riley",
            riderLastName: "Carter",
            pickupLocation: "Hartsfield Airport",
            dropoffLocation: "Downtown Atlanta");
        var otherRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-combined-other-driver@test.com",
            riderEmail: "filter-combined-other-rider@test.com",
            daysFromNow: 11,
            driverFirstName: "Marcus",
            driverLastName: "Reed",
            riderFirstName: "Taylor",
            riderLastName: "Morgan",
            pickupLocation: "Buckhead",
            dropoffLocation: "Decatur");

        var statusResponse = await _client.PatchAsync(
            $"/ride-requests/{matchingRideRequest.Id}/status?status={RideRequestStatus.Accepted}",
            content: null);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync(
            "/ride-requests?driverName=serena&riderName=carter&status=Accepted&pickupLocation=airport&dropoffLocation=downtown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r => r.Id == matchingRideRequest.Id);
        rideRequests.Should().NotContain(r => r.Id == otherRideRequest.Id);
    }

    [Fact]
    public async Task GetRideRequests_ShouldFilterByAvailabilitySlotAndSlotTimeRanges()
    {
        var matchingSlotStartTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var otherSlotStartTime = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);
        var matchingRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-slot-time-driver@test.com",
            riderEmail: "filter-slot-time-rider@test.com",
            slotStartTime: matchingSlotStartTime);
        var otherRideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-slot-time-other-driver@test.com",
            riderEmail: "filter-slot-time-other-rider@test.com",
            slotStartTime: otherSlotStartTime);

        var response = await _client.GetAsync(
            $"/ride-requests?availabilitySlotId={matchingRideRequest.AvailabilitySlotId}&slotStartTimeFrom=2026-06-15T00:00:00Z&slotStartTimeTo=2026-06-15T23:59:59Z&slotEndTimeFrom=2026-06-15T09:30:00Z&slotEndTimeTo=2026-06-15T10:30:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r => r.Id == matchingRideRequest.Id);
        rideRequests.Should().NotContain(r => r.Id == otherRideRequest.Id);
    }

    [Fact]
    public async Task GetRideRequests_ShouldFilterByCreatedAtRange()
    {
        var createdAtFrom = DateTime.UtcNow.AddMinutes(-1).ToString("O");
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "filter-created-driver@test.com",
            riderEmail: "filter-created-rider@test.com",
            daysFromNow: 12);
        var createdAtTo = DateTime.UtcNow.AddMinutes(1).ToString("O");

        var response = await _client.GetAsync(
            $"/ride-requests?createdAtFrom={Uri.EscapeDataString(createdAtFrom)}&createdAtTo={Uri.EscapeDataString(createdAtTo)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests.Should().Contain(r => r.Id == rideRequest.Id);
    }

    [Fact]
    public async Task GetRideRequests_ShouldFilterUpcomingAcceptedRides()
    {
        var driver = await CreateUserAsync("upcoming-accepted-driver@test.com", role: 1);
        var rider = await CreateUserAsync("upcoming-accepted-rider@test.com", role: 0);
        var pastRide = await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddHours(-2));
        var upcomingRide = await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddHours(2));
        var laterUpcomingRide = await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddHours(3));
        var pendingUpcomingRide = await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddHours(4));

        await _client.PatchAsync($"/ride-requests/{pastRide.Id}/status?status={RideRequestStatus.Accepted}", content: null);
        await _client.PatchAsync($"/ride-requests/{upcomingRide.Id}/status?status={RideRequestStatus.Accepted}", content: null);
        await _client.PatchAsync($"/ride-requests/{laterUpcomingRide.Id}/status?status={RideRequestStatus.Accepted}", content: null);

        var response = await _client.GetAsync(
            $"/ride-requests?driverId={driver.Id}&status=Accepted&upcomingOnly=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);
        var rideRequests = rideRequestPage.Items;

        rideRequests.Should().NotBeNull();
        rideRequests!.Select(r => r.Id).Should().Equal(upcomingRide.Id, laterUpcomingRide.Id);
        rideRequests.Should().NotContain(r => r.Id == pastRide.Id);
        rideRequests.Should().NotContain(r => r.Id == pendingUpcomingRide.Id);
    }

    [Fact]
    public async Task GetRideRequests_ShouldReturnPaginatedRideRequests()
    {
        var driver = await CreateUserAsync("pagination-driver@test.com", role: 1);
        var rider = await CreateUserAsync("pagination-rider@test.com", role: 0);

        await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddDays(20));
        await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddDays(21));
        await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddDays(22));

        var response = await _client.GetAsync($"/ride-requests?driverId={driver.Id}&page=2&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rideRequestPage = await ReadRideRequestPageAsync(response);

        rideRequestPage.Page.Should().Be(2);
        rideRequestPage.PageSize.Should().Be(2);
        rideRequestPage.TotalItems.Should().Be(3);
        rideRequestPage.TotalPages.Should().Be(2);
        rideRequestPage.HasPreviousPage.Should().BeTrue();
        rideRequestPage.HasNextPage.Should().BeFalse();
        rideRequestPage.Items.Should().HaveCount(1);
        rideRequestPage.Items.Should().OnlyContain(r => r.DriverId == driver.Id);
    }

    [Fact]
    public async Task PutRideRequest_ShouldUpdateEditableFieldsAndMoveBookedSlot()
    {
        var originalDriver = await CreateUserAsync("put-original-driver@test.com", role: 1);
        var originalRider = await CreateUserAsync("put-original-rider@test.com", role: 0);
        var originalRideRequest = await CreateRideRequestAsync(
            originalDriver,
            originalRider,
            DateTime.UtcNow.AddDays(30));

        var updatedDriver = await CreateUserAsync(
            "put-updated-driver@test.com",
            role: 1,
            firstName: "Updated",
            lastName: "Driver");
        var updatedRider = await CreateUserAsync(
            "put-updated-rider@test.com",
            role: 0,
            firstName: "Updated",
            lastName: "Rider");
        var updatedSlotStartTime = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);
        var updatedSlot = await CreateAvailabilitySlotAsync(updatedDriver.Id, updatedSlotStartTime);
        var updatedCreatedAt = new DateTime(2026, 7, 19, 12, 30, 0, DateTimeKind.Utc);
        var updatedCompletedAt = new DateTime(2026, 7, 20, 15, 5, 0, DateTimeKind.Utc);

        var response = await _client.PutAsJsonAsync(
            $"/ride-requests/{originalRideRequest.Id}",
            new UpdateRideRequestDto
            {
                RiderId = updatedRider.Id,
                DriverId = updatedDriver.Id,
                AvailabilitySlotId = updatedSlot.Id,
                PickupLocation = "Georgia Aquarium",
                DropoffLocation = "Fox Theatre",
                FareAmount = 84.25m,
                DriverEarningsAmount = 72.50m,
                Status = RideRequestStatus.Completed,
                CreatedAt = updatedCreatedAt,
                CompletedAt = updatedCompletedAt
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        updatedRideRequest.Should().NotBeNull();
        updatedRideRequest!.Id.Should().Be(originalRideRequest.Id);
        updatedRideRequest.RiderId.Should().Be(updatedRider.Id);
        updatedRideRequest.RiderName.Should().Be("Updated Rider");
        updatedRideRequest.DriverId.Should().Be(updatedDriver.Id);
        updatedRideRequest.DriverName.Should().Be("Updated Driver");
        updatedRideRequest.AvailabilitySlotId.Should().Be(updatedSlot.Id);
        updatedRideRequest.SlotStartTime.Should().Be(updatedSlotStartTime);
        updatedRideRequest.PickupLocation.Should().Be("Georgia Aquarium");
        updatedRideRequest.DropoffLocation.Should().Be("Fox Theatre");
        updatedRideRequest.FareAmount.Should().Be(84.25m);
        updatedRideRequest.DriverEarningsAmount.Should().Be(72.50m);
        updatedRideRequest.Status.Should().Be("Completed");
        updatedRideRequest.CreatedAt.Should().Be(updatedCreatedAt);
        updatedRideRequest.CompletedAt.Should().Be(updatedCompletedAt);

        var availabilityResponse = await _client.GetAsync("/availability");
        var availableSlots = await availabilityResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        availableSlots.Should().Contain(s => s.Id == originalRideRequest.AvailabilitySlotId);
        availableSlots.Should().NotContain(s => s.Id == updatedSlot.Id);
    }

    [Fact]
    public async Task PutRideRequest_ShouldReturnBadRequest_WhenAvailabilitySlotIsBookedByAnotherRide()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "put-booked-driver@test.com",
            riderEmail: "put-booked-rider@test.com",
            daysFromNow: 31);
        var otherRideRequest = await CreateRideRequestAsync(
            driverEmail: "put-booked-other-driver@test.com",
            riderEmail: "put-booked-other-rider@test.com",
            daysFromNow: 32);

        var response = await _client.PutAsJsonAsync(
            $"/ride-requests/{rideRequest.Id}",
            new UpdateRideRequestDto
            {
                RiderId = rideRequest.RiderId,
                DriverId = otherRideRequest.DriverId,
                AvailabilitySlotId = otherRideRequest.AvailabilitySlotId,
                PickupLocation = rideRequest.PickupLocation,
                DropoffLocation = rideRequest.DropoffLocation,
                FareAmount = rideRequest.FareAmount,
                DriverEarningsAmount = rideRequest.DriverEarningsAmount,
                Status = RideRequestStatus.Pending,
                CreatedAt = rideRequest.CreatedAt,
                CompletedAt = rideRequest.CompletedAt
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutRideRequest_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "put-forbidden-driver@test.com",
            riderEmail: "put-forbidden-rider@test.com",
            daysFromNow: 33);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/ride-requests/{rideRequest.Id}")
        {
            Content = JsonContent.Create(new UpdateRideRequestDto
            {
                RiderId = rideRequest.RiderId,
                DriverId = rideRequest.DriverId,
                AvailabilitySlotId = rideRequest.AvailabilitySlotId,
                PickupLocation = rideRequest.PickupLocation,
                DropoffLocation = rideRequest.DropoffLocation,
                FareAmount = rideRequest.FareAmount,
                DriverEarningsAmount = rideRequest.DriverEarningsAmount,
                Status = RideRequestStatus.Pending,
                CreatedAt = rideRequest.CreatedAt,
                CompletedAt = rideRequest.CompletedAt
            })
        };
        request.Headers.Add(TestAuthHandler.UserRoleHeaderName, UserRole.Rider.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostRideRequest_ShouldReturnNotFound_WhenAvailabilitySlotDoesNotExist()
    {
        var driver = await CreateUserAsync("missing-slot-driver@test.com", role: 1);
        var rider = await CreateUserAsync("missing-slot-rider@test.com", role: 0);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
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

    [Fact]
    public async Task PostRideRequest_ShouldCreateNotificationForDriver()
    {
        var driver = await CreateUserAsync("notification-create-driver@test.com", role: 1);
        var rider = await CreateUserAsync("notification-create-rider@test.com", role: 0);
        var rideRequest = await CreateRideRequestAsync(driver, rider, DateTime.UtcNow.AddDays(12));

        var notification = await GetNotificationAsync(driver.Id, rideRequest.Id, NotificationType.RideRequested);

        notification.Should().NotBeNull();
        notification!.Title.Should().Be("New ride request");
        notification.Message.Should().Be("You have a new ride request.");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldCreateAcceptedNotificationForRider()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "notification-accepted-driver@test.com",
            riderEmail: "notification-accepted-rider@test.com",
            daysFromNow: 13);

        var response = await _client.PatchAsync(
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Accepted}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var notification = await GetNotificationAsync(rideRequest.RiderId, rideRequest.Id, NotificationType.RideAccepted);

        notification.Should().NotBeNull();
        notification!.Message.Should().Be("Your ride request was accepted.");
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldCreateDeclinedNotificationForRider()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "notification-declined-driver@test.com",
            riderEmail: "notification-declined-rider@test.com",
            daysFromNow: 14);

        var response = await _client.PatchAsync(
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Declined}",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var notification = await GetNotificationAsync(rideRequest.RiderId, rideRequest.Id, NotificationType.RideDenied);

        notification.Should().NotBeNull();
        notification!.Message.Should().Be("Your ride request was declined.");
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldNotifyDriver_WhenRiderCancelsRide()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "notification-rider-cancel-driver@test.com",
            riderEmail: "notification-rider-cancel-rider@test.com",
            daysFromNow: 15);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Cancelled}");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, rideRequest.RiderId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var notification = await GetNotificationAsync(rideRequest.DriverId, rideRequest.Id, NotificationType.RideCancelled);

        notification.Should().NotBeNull();
        notification!.Message.Should().Be("The rider canceled the ride.");
    }

    [Fact]
    public async Task PatchRideRequestStatus_ShouldNotifyRider_WhenDriverCancelsRide()
    {
        var rideRequest = await CreateRideRequestAsync(
            driverEmail: "notification-driver-cancel-driver@test.com",
            riderEmail: "notification-driver-cancel-rider@test.com",
            daysFromNow: 16);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/ride-requests/{rideRequest.Id}/status?status={RideRequestStatus.Cancelled}");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, rideRequest.DriverId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var notification = await GetNotificationAsync(rideRequest.RiderId, rideRequest.Id, NotificationType.RideCancelled);

        notification.Should().NotBeNull();
        notification!.Message.Should().Be("Your driver canceled the ride.");
    }

    private async Task<UserResponseDto> CreateUserAsync(
        string email,
        int role,
        string? firstName = null,
        string? lastName = null)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = firstName ?? (role == 1 ? "Driver" : "Rider"),
            LastName = lastName ?? "User",
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
        var startTime = DateTime.UtcNow.AddDays(daysFromNow);
        var response = await _client.PostAsJsonAsync("/availability", new CreateAvailabilitySlotDto
        {
            DriverId = driverId,
            StartTime = startTime,
            EndTime = startTime.AddHours(1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        return slots!.Single();
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

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponseDto>>();

        return slots!.Single();
    }

    private async Task<RideRequestResponseDto> CreateRideRequestAsync(
        string driverEmail,
        string riderEmail,
        int daysFromNow = 1,
        DateTime? slotStartTime = null,
        string? driverFirstName = null,
        string? driverLastName = null,
        string? riderFirstName = null,
        string? riderLastName = null,
        string pickupLocation = "Location A",
        string dropoffLocation = "Location B")
    {
        var driver = await CreateUserAsync(driverEmail, role: 1, driverFirstName, driverLastName);
        var rider = await CreateUserAsync(riderEmail, role: 0, riderFirstName, riderLastName);
        var slot = slotStartTime.HasValue
            ? await CreateAvailabilitySlotAsync(driver.Id, slotStartTime.Value)
            : await CreateAvailabilitySlotAsync(driver.Id, daysFromNow);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = pickupLocation,
            DropoffLocation = dropoffLocation
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        return rideRequest!;
    }

    private async Task<RideRequestResponseDto> CreateRideRequestAsync(
        UserResponseDto driver,
        UserResponseDto rider,
        DateTime slotStartTime)
    {
        var slot = await CreateAvailabilitySlotAsync(driver.Id, slotStartTime);

        var response = await _client.PostAsJsonAsync("/ride-requests", new CreateRideRequestDto
        {
            RiderId = rider.Id,
            AvailabilitySlotId = slot.Id,
            PickupLocation = "Location A",
            DropoffLocation = "Location B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rideRequest = await response.Content.ReadFromJsonAsync<RideRequestResponseDto>();

        return rideRequest!;
    }

    private static async Task<PaginatedResponseDto<RideRequestResponseDto>> ReadRideRequestPageAsync(
        HttpResponseMessage response)
    {
        var rideRequestPage = await response.Content
            .ReadFromJsonAsync<PaginatedResponseDto<RideRequestResponseDto>>();

        rideRequestPage.Should().NotBeNull();

        return rideRequestPage!;
    }

    private async Task<Notification?> GetNotificationAsync(
        Guid userId,
        Guid rideId,
        NotificationType notificationType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Notifications
            .Where(n => n.UserId == userId &&
                n.RideId == rideId &&
                n.NotificationType == notificationType)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

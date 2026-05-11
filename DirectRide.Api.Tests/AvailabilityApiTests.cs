using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.AvailabilitySlots;
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
}

using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
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
}
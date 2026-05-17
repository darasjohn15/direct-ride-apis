using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.Data;
using DirectRide.Api.DTOs.Earnings;
using DirectRide.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DirectRide.Api.Tests;

public class EarningsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public EarningsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDailyEarnings_ShouldReturnCompletedRideTotalsForDate()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 12, 9, 0, 0), 92.25m),
            new SeedRideEarning(new DateTime(2026, 5, 12, 15, 30, 0), 92.25m),
            new SeedRideEarning(new DateTime(2026, 5, 13, 9, 0, 0), 48.00m)
        });

        var response = await _client.GetAsync($"/api/earnings/drivers/{driverId}/daily?date=2026-05-12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var earnings = await response.Content.ReadFromJsonAsync<DailyEarningsResponseDto>();

        earnings.Should().NotBeNull();
        earnings!.Date.Should().Be(new DateOnly(2026, 5, 12));
        earnings.DayLabel.Should().Be("Tuesday");
        earnings.TotalEarnings.Should().Be(184.50m);
        earnings.TotalRides.Should().Be(2);
    }

    [Fact]
    public async Task GetDailyEarnings_ShouldSupportProxyStrippedRoute()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 12, 9, 0, 0), 184.50m)
        });

        var response = await _client.GetAsync($"/earnings/drivers/{driverId}/daily?date=2026-05-12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDailyEarnings_ShouldReturnZeroTotals_WhenDriverHasNoCompletedRidesForDate()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 11, 9, 0, 0), 72.00m),
            new SeedRideEarning(new DateTime(2026, 5, 13, 9, 0, 0), 48.00m)
        });

        var response = await _client.GetAsync($"/api/earnings/drivers/{driverId}/daily?date=2026-05-12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var earnings = await response.Content.ReadFromJsonAsync<DailyEarningsResponseDto>();

        earnings.Should().NotBeNull();
        earnings!.Date.Should().Be(new DateOnly(2026, 5, 12));
        earnings.TotalEarnings.Should().Be(0.00m);
        earnings.TotalRides.Should().Be(0);
    }

    [Fact]
    public async Task GetDailyEarnings_ShouldOnlyIncludeCompletedRidesWithCompletedAt()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 12, 9, 0, 0), 92.25m),
            new SeedRideEarning(new DateTime(2026, 5, 12, 10, 0, 0), 50.00m, RideRequestStatus.Pending),
            new SeedRideEarning(new DateTime(2026, 5, 12, 11, 0, 0), 60.00m, RideRequestStatus.Accepted),
            new SeedRideEarning(new DateTime(2026, 5, 12, 12, 0, 0), 70.00m, RideRequestStatus.Cancelled),
            new SeedRideEarning(new DateTime(2026, 5, 12, 13, 0, 0), 80.00m, RideRequestStatus.Completed, HasCompletedAt: false)
        });

        var response = await _client.GetAsync($"/api/earnings/drivers/{driverId}/daily?date=2026-05-12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var earnings = await response.Content.ReadFromJsonAsync<DailyEarningsResponseDto>();

        earnings.Should().NotBeNull();
        earnings!.TotalEarnings.Should().Be(92.25m);
        earnings.TotalRides.Should().Be(1);
    }

    [Fact]
    public async Task GetWeeklyEarnings_ShouldReturnWeekTotalsAndDailyBreakdown()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 11, 9, 0, 0), 92.50m),
            new SeedRideEarning(new DateTime(2026, 5, 11, 11, 0, 0), 25.00m),
            new SeedRideEarning(new DateTime(2026, 5, 12, 14, 0, 0), 184.50m),
            new SeedRideEarning(new DateTime(2026, 5, 17, 18, 0, 0), 75.25m),
            new SeedRideEarning(new DateTime(2026, 5, 18, 9, 0, 0), 99.00m)
        });

        var response = await _client.GetAsync($"/api/earnings/drivers/{driverId}/weekly?weekStart=2026-05-11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var earnings = await response.Content.ReadFromJsonAsync<WeeklyEarningsResponseDto>();

        earnings.Should().NotBeNull();
        earnings!.WeekStart.Should().Be(new DateOnly(2026, 5, 11));
        earnings.WeekEnd.Should().Be(new DateOnly(2026, 5, 17));
        earnings.TotalEarnings.Should().Be(377.25m);
        earnings.TotalRides.Should().Be(4);
        earnings.Days.Should().HaveCount(7);
        earnings.Days[0].Date.Should().Be(new DateOnly(2026, 5, 11));
        earnings.Days[0].DayLabel.Should().Be("Monday");
        earnings.Days[0].TotalEarnings.Should().Be(117.50m);
        earnings.Days[0].TotalRides.Should().Be(2);
        earnings.Days[1].TotalEarnings.Should().Be(184.50m);
        earnings.Days[6].TotalEarnings.Should().Be(75.25m);
    }

    [Fact]
    public async Task GetWeeklyEarnings_ShouldSupportProxyStrippedRoute()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 12, 9, 0, 0), 184.50m)
        });

        var response = await _client.GetAsync($"/earnings/drivers/{driverId}/weekly?weekStart=2026-05-11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWeeklyEarnings_ShouldReturnSevenZeroDays_WhenDriverHasNoCompletedRidesInWeek()
    {
        var driverId = await SeedDriverEarningsAsync(new[]
        {
            new SeedRideEarning(new DateTime(2026, 5, 10, 23, 59, 0), 72.00m),
            new SeedRideEarning(new DateTime(2026, 5, 18, 0, 0, 0), 48.00m)
        });

        var response = await _client.GetAsync($"/api/earnings/drivers/{driverId}/weekly?weekStart=2026-05-11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var earnings = await response.Content.ReadFromJsonAsync<WeeklyEarningsResponseDto>();

        earnings.Should().NotBeNull();
        earnings!.WeekStart.Should().Be(new DateOnly(2026, 5, 11));
        earnings.WeekEnd.Should().Be(new DateOnly(2026, 5, 17));
        earnings.TotalEarnings.Should().Be(0.00m);
        earnings.TotalRides.Should().Be(0);
        earnings.Days.Should().HaveCount(7);
        earnings.Days.Should().OnlyContain(day => day.TotalEarnings == 0.00m && day.TotalRides == 0);
        earnings.Days.Select(day => day.Date).Should().Equal(
            new DateOnly(2026, 5, 11),
            new DateOnly(2026, 5, 12),
            new DateOnly(2026, 5, 13),
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 15),
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 5, 17));
    }

    [Fact]
    public async Task GetDailyEarnings_ShouldReturnNotFound_WhenDriverDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/earnings/drivers/{Guid.NewGuid()}/daily?date=2026-05-12");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWeeklyEarnings_ShouldReturnNotFound_WhenDriverDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/earnings/drivers/{Guid.NewGuid()}/weekly?weekStart=2026-05-11");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedDriverEarningsAsync(IEnumerable<SeedRideEarning> rideEarnings)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var driver = new User
        {
            FirstName = "Earnings",
            LastName = "Driver",
            Email = $"earnings-driver-{Guid.NewGuid()}@test.com",
            PhoneNumber = "555-777-0000",
            Role = UserRole.Driver,
            BaseFare = 42.00m,
            PasswordHash = "test-password-hash"
        };

        var rider = new User
        {
            FirstName = "Earnings",
            LastName = "Rider",
            Email = $"earnings-rider-{Guid.NewGuid()}@test.com",
            PhoneNumber = "555-777-1111",
            Role = UserRole.Rider,
            PasswordHash = "test-password-hash"
        };

        db.Users.AddRange(driver, rider);

        foreach (var rideEarning in rideEarnings)
        {
            var slot = new AvailabilitySlot
            {
                DriverId = driver.Id,
                StartTime = rideEarning.CompletedAt.AddHours(-1),
                EndTime = rideEarning.CompletedAt,
                IsBooked = true
            };

            db.AvailabilitySlots.Add(slot);
            db.RideRequests.Add(new RideRequest
            {
                RiderId = rider.Id,
                DriverId = driver.Id,
                AvailabilitySlotId = slot.Id,
                PickupLocation = "Pickup",
                DropoffLocation = "Dropoff",
                FareAmount = rideEarning.DriverEarningsAmount,
                DriverEarningsAmount = rideEarning.DriverEarningsAmount,
                Status = rideEarning.Status,
                CompletedAt = rideEarning.HasCompletedAt ? rideEarning.CompletedAt : null
            });
        }

        await db.SaveChangesAsync();

        return driver.Id;
    }

    private sealed record SeedRideEarning(
        DateTime CompletedAt,
        decimal DriverEarningsAmount,
        RideRequestStatus Status = RideRequestStatus.Completed,
        bool HasCompletedAt = true);
}

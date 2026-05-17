using DirectRide.Api.Services;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class EarningsServiceTests
{
    [Fact]
    public void CreateDailyResponse_ShouldSumOnlyRidesCompletedOnRequestedDate()
    {
        var rideSummaries = new[]
        {
            new RideEarningSummary(new DateTime(2026, 5, 11, 23, 59, 0), 50.00m),
            new RideEarningSummary(new DateTime(2026, 5, 12, 9, 0, 0), 92.25m),
            new RideEarningSummary(new DateTime(2026, 5, 12, 15, 30, 0), 92.25m),
            new RideEarningSummary(new DateTime(2026, 5, 13, 0, 0, 0), 48.00m)
        };

        var response = EarningsService.CreateDailyResponse(new DateOnly(2026, 5, 12), rideSummaries);

        response.Date.Should().Be(new DateOnly(2026, 5, 12));
        response.DayLabel.Should().Be("Tuesday");
        response.TotalEarnings.Should().Be(184.50m);
        response.TotalRides.Should().Be(2);
    }

    [Fact]
    public void CreateDailyResponse_ShouldReturnZeroTotals_WhenNoRidesMatchDate()
    {
        var rideSummaries = new[]
        {
            new RideEarningSummary(new DateTime(2026, 5, 11, 9, 0, 0), 50.00m),
            new RideEarningSummary(new DateTime(2026, 5, 13, 9, 0, 0), 48.00m)
        };

        var response = EarningsService.CreateDailyResponse(new DateOnly(2026, 5, 12), rideSummaries);

        response.Date.Should().Be(new DateOnly(2026, 5, 12));
        response.DayLabel.Should().Be("Tuesday");
        response.TotalEarnings.Should().Be(0.00m);
        response.TotalRides.Should().Be(0);
    }

    [Fact]
    public void CreateWeeklyResponse_ShouldReturnWeekTotalsAndSevenDayBreakdown()
    {
        var rideSummaries = new[]
        {
            new RideEarningSummary(new DateTime(2026, 5, 11, 9, 0, 0), 92.50m),
            new RideEarningSummary(new DateTime(2026, 5, 11, 11, 0, 0), 25.00m),
            new RideEarningSummary(new DateTime(2026, 5, 12, 14, 0, 0), 184.50m),
            new RideEarningSummary(new DateTime(2026, 5, 17, 18, 0, 0), 75.25m)
        };

        var response = EarningsService.CreateWeeklyResponse(new DateOnly(2026, 5, 11), rideSummaries);

        response.WeekStart.Should().Be(new DateOnly(2026, 5, 11));
        response.WeekEnd.Should().Be(new DateOnly(2026, 5, 17));
        response.TotalEarnings.Should().Be(377.25m);
        response.TotalRides.Should().Be(4);
        response.Days.Should().HaveCount(7);
        response.Days[0].Date.Should().Be(new DateOnly(2026, 5, 11));
        response.Days[0].DayLabel.Should().Be("Monday");
        response.Days[0].TotalEarnings.Should().Be(117.50m);
        response.Days[0].TotalRides.Should().Be(2);
        response.Days[1].TotalEarnings.Should().Be(184.50m);
        response.Days[6].TotalEarnings.Should().Be(75.25m);
    }

    [Fact]
    public void CreateWeeklyResponse_ShouldReturnSevenZeroDays_WhenNoRidesMatchWeek()
    {
        var response = EarningsService.CreateWeeklyResponse(
            new DateOnly(2026, 5, 11),
            Enumerable.Empty<RideEarningSummary>());

        response.WeekStart.Should().Be(new DateOnly(2026, 5, 11));
        response.WeekEnd.Should().Be(new DateOnly(2026, 5, 17));
        response.TotalEarnings.Should().Be(0.00m);
        response.TotalRides.Should().Be(0);
        response.Days.Should().HaveCount(7);
        response.Days.Should().OnlyContain(day => day.TotalEarnings == 0.00m && day.TotalRides == 0);
    }
}

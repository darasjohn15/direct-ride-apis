using DirectRide.Api.DTOs.Earnings;

namespace DirectRide.Api.Services;

public static class EarningsService
{
    public static DailyEarningsResponseDto CreateDailyResponse(
        DateOnly date,
        IEnumerable<RideEarningSummary> rideSummaries)
    {
        var matchingRides = rideSummaries
            .Where(r => DateOnly.FromDateTime(r.CompletedAt) == date)
            .ToList();

        return new DailyEarningsResponseDto
        {
            Date = date,
            DayLabel = date.DayOfWeek.ToString(),
            TotalEarnings = matchingRides.Sum(r => r.DriverEarningsAmount),
            TotalRides = matchingRides.Count
        };
    }

    public static WeeklyEarningsResponseDto CreateWeeklyResponse(
        DateOnly weekStart,
        IEnumerable<RideEarningSummary> rideSummaries)
    {
        var rideSummaryList = rideSummaries.ToList();
        var weekEnd = weekStart.AddDays(6);
        var days = Enumerable.Range(0, 7)
            .Select(offset => CreateDailyResponse(weekStart.AddDays(offset), rideSummaryList))
            .ToList();

        return new WeeklyEarningsResponseDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            TotalEarnings = days.Sum(d => d.TotalEarnings),
            TotalRides = days.Sum(d => d.TotalRides),
            Days = days
        };
    }
}

public sealed record RideEarningSummary(DateTime CompletedAt, decimal DriverEarningsAmount);

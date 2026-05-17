using DirectRide.Api.Data;
using DirectRide.Api.Models;
using DirectRide.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Endpoints;

public static class EarningsEndpoints
{
    public static IEndpointRouteBuilder MapEarningsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEarningsGroup("/api/earnings");
        app.MapEarningsGroup("/earnings");

        return app;
    }

    private static IEndpointRouteBuilder MapEarningsGroup(this IEndpointRouteBuilder app, string prefix)
    {
        var group = app.MapGroup(prefix)
            .RequireAuthorization();

        group.MapGet("/drivers/{driverId:guid}/daily", async (AppDbContext db, Guid driverId, DateOnly date) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == driverId && u.Role == UserRole.Driver))
            {
                return Results.NotFound("Driver not found.");
            }

            var rideSummaries = await GetCompletedRideSummariesAsync(db, driverId, date, date.AddDays(1));

            var response = EarningsService.CreateDailyResponse(date, rideSummaries);

            return Results.Ok(response);
        });

        group.MapGet("/drivers/{driverId:guid}/weekly", async (AppDbContext db, Guid driverId, DateOnly weekStart) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == driverId && u.Role == UserRole.Driver))
            {
                return Results.NotFound("Driver not found.");
            }

            var rideSummaries = await GetCompletedRideSummariesAsync(db, driverId, weekStart, weekStart.AddDays(7));
            var response = EarningsService.CreateWeeklyResponse(weekStart, rideSummaries);

            return Results.Ok(response);
        });

        return app;
    }

    private static async Task<List<RideEarningSummary>> GetCompletedRideSummariesAsync(
        AppDbContext db,
        Guid driverId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var rangeStart = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEnd = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await db.RideRequests
            .Where(r =>
                r.DriverId == driverId
                && r.Status == RideRequestStatus.Completed
                && r.CompletedAt.HasValue
                && r.CompletedAt.Value >= rangeStart
                && r.CompletedAt.Value < rangeEnd)
            .Select(r => new RideEarningSummary
            (
                r.CompletedAt!.Value,
                r.DriverEarningsAmount
            ))
            .ToListAsync();
    }
}

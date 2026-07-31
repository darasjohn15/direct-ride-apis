using DirectRide.Api.DTOs.AvailabilitySlots;
using DirectRide.Api.Services;

namespace DirectRide.Api.Endpoints;

public static class AvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/availability")
            .RequireAuthorization();

        group.MapGet("", async (
            AvailabilityService availabilityService,
            [AsParameters] AvailabilitySlotFilterDto filters,
            CancellationToken cancellationToken) =>
        {
            var slots = await availabilityService.GetAvailabilitySlotsAsync(filters, cancellationToken);

            return Results.Ok(slots);
        });

        group.MapPost("", async (
            AvailabilityService availabilityService,
            CreateAvailabilitySlotDto dto,
            CancellationToken cancellationToken) =>
        {
            var result = await availabilityService.CreateAvailabilitySlotsAsync(dto, cancellationToken);

            return result.Status switch
            {
                AvailabilityServiceResultStatus.Created => Results.Created("/availability", result.Value),
                AvailabilityServiceResultStatus.NotFound => Results.NotFound(result.Error),
                AvailabilityServiceResultStatus.BadRequest => Results.BadRequest(result.Error),
                _ => Results.Problem("Unexpected availability service result.")
            };
        });

        return app;
    }
}

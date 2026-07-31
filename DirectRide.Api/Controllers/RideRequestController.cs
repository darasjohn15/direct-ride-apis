using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DirectRide.Api.DTOs.RideRequests;
using DirectRide.Api.Models;
using DirectRide.Api.Services;

namespace DirectRide.Api.Controllers;

public static class RideRequestController
{
    public static IEndpointRouteBuilder MapRideRequestController(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ride-requests")
            .RequireAuthorization();

        group.MapGet("", async (
            RideRequestService rideRequestService,
            [AsParameters] RideRequestFilterDto filters,
            CancellationToken cancellationToken) =>
        {
            var rideRequests = await rideRequestService.GetRideRequestsAsync(filters, cancellationToken);

            return Results.Ok(rideRequests);
        });

        group.MapGet("/{id:guid}", async (
            RideRequestService rideRequestService,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var result = await rideRequestService.GetRideRequestAsync(id, cancellationToken);

            return ToHttpResult(result);
        });

        group.MapPost("", async (
            RideRequestService rideRequestService,
            CreateRideRequestDto dto,
            CancellationToken cancellationToken) =>
        {
            var result = await rideRequestService.CreateRideRequestAsync(dto, cancellationToken);

            return result.Status == RideRequestServiceResultStatus.Created
                ? Results.Created($"/ride-requests/{result.Value?.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapPut("/{id:guid}", async (
            ClaimsPrincipal claimsPrincipal,
            RideRequestService rideRequestService,
            Guid id,
            UpdateRideRequestDto dto,
            CancellationToken cancellationToken) =>
        {
            TryGetUserId(claimsPrincipal, out var actorUserId);
            var result = await rideRequestService.UpdateRideRequestAsync(id, dto, actorUserId, cancellationToken);

            return ToHttpResult(result);
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPatch("/{id:guid}/start", async (
            RideRequestService rideRequestService,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var result = await rideRequestService.StartRideRequestAsync(id, cancellationToken);

            return ToHttpResult(result);
        });

        group.MapPatch("/{id:guid}/complete", async (
            RideRequestService rideRequestService,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var result = await rideRequestService.CompleteRideRequestAsync(id, cancellationToken);

            return ToHttpResult(result);
        });

        group.MapPatch("/{id:guid}/cancel", async (
            ClaimsPrincipal claimsPrincipal,
            RideRequestService rideRequestService,
            Guid id,
            string? cancellationReason,
            CancellationToken cancellationToken) =>
        {
            TryGetUserId(claimsPrincipal, out var actorUserId);
            var result = await rideRequestService.CancelRideRequestAsync(
                id,
                actorUserId,
                cancellationReason,
                cancellationToken);

            return ToHttpResult(result);
        });

        group.MapPatch("/{id}/status", async (
            ClaimsPrincipal claimsPrincipal,
            RideRequestService rideRequestService,
            Guid id,
            RideRequestStatus status,
            CancellationToken cancellationToken) =>
        {
            TryGetUserId(claimsPrincipal, out var actorUserId);
            var result = await rideRequestService.UpdateRideRequestStatusAsync(
                id,
                status,
                actorUserId,
                cancellationToken);

            return ToHttpResult(result);
        });

        return app;
    }

    private static IResult ToHttpResult<T>(RideRequestServiceResult<T> result)
    {
        return result.Status switch
        {
            RideRequestServiceResultStatus.Ok => Results.Ok(result.Value),
            RideRequestServiceResultStatus.NotFound => Results.NotFound(result.Error),
            RideRequestServiceResultStatus.BadRequest => Results.BadRequest(result.Error),
            RideRequestServiceResultStatus.Created => Results.Created(string.Empty, result.Value),
            _ => Results.Problem("Unexpected ride request service result.")
        };
    }

    private static bool TryGetUserId(ClaimsPrincipal claimsPrincipal, out Guid userId)
    {
        var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdClaim, out userId);
    }
}

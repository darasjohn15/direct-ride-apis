namespace DirectRide.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AvailabilitySlot> AvailabilitySlots { get; set; } = new();
    public List<RideRequest> RiderRideRequests { get; set; } = new();

    public List<RideRequest> DriverRideRequests { get; set; } = new();
}
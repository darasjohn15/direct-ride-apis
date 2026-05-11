namespace DirectRide.Api.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public decimal BaseFare { get; set; } = 0.00m;
}
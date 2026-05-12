namespace DirectRide.Api.DTOs;

public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public int Role { get; set; }

    public decimal BaseFare { get; set; } = 0.00m;
}

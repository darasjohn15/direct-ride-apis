namespace DirectRide.Api.DTOs;

public class PatchUserDto
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public int? Role { get; set; }

    public decimal? BaseFare { get; set; }
}

namespace DirectRide.Api.DTOs.Earnings;

public class DailyEarningsResponseDto
{
    public DateOnly Date { get; set; }

    public string DayLabel { get; set; } = string.Empty;

    public decimal TotalEarnings { get; set; }

    public int TotalRides { get; set; }
}

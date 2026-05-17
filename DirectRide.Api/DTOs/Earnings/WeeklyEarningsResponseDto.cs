namespace DirectRide.Api.DTOs.Earnings;

public class WeeklyEarningsResponseDto
{
    public DateOnly WeekStart { get; set; }

    public DateOnly WeekEnd { get; set; }

    public decimal TotalEarnings { get; set; }

    public int TotalRides { get; set; }

    public List<DailyEarningsResponseDto> Days { get; set; } = new();
}

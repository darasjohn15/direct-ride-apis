using Microsoft.AspNetCore.Mvc;

namespace DirectRide.Api.DTOs.Notifications;

public class NotificationQueryDto
{
    public int? Page { get; set; }

    [FromQuery(Name = "page_size")]
    public int? PageSize { get; set; }
}

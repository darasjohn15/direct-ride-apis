namespace DirectRide.Api.DTOs;

public class PaginatedResponseDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage { get; set; }

    public bool HasNextPage { get; set; }
}

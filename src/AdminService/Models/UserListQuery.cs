namespace AdminService.Models;

public sealed class UserListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

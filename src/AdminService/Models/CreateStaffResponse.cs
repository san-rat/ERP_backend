namespace AdminService.Models;

public sealed class CreateStaffResponse
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? FullName { get; init; }
    public required string Role { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required string Password { get; init; }
}

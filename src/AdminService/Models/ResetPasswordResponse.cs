namespace AdminService.Models;

public sealed class ResetPasswordResponse
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

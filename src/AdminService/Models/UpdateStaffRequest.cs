namespace AdminService.Models;

public record UpdateStaffRequest(
    string Username,
    string Email,
    string? FullName,
    string Role);

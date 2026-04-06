namespace AdminService.Models;

public record CreateStaffRequest(
    string Username,
    string Email,
    string? FullName = null);

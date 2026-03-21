namespace AuthService.Services;

public interface IUserRepository
{
    Task<DbUser?> FindByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string? fullName, string roleName);
}

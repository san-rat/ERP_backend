using AdminService.Models;
using AdminService.Repositories;
using AdminService.Services;

namespace AdminService.Tests.Services;

public class AdminUserServiceTests
{
    [Fact]
    public async Task CreateStaffAsync_WithManagerRole_CreatesCanonicalManagerAndReturnsPassword()
    {
        var repository = new FakeAdminUserRepository();
        var service = new AdminUserService(repository, new FixedPasswordGenerator("Temp@12345"));

        var response = await service.CreateStaffAsync(
            new CreateStaffRequest("manager.new", "manager.new@test.com", "Manager New"),
            RoleNormalizer.Manager);

        Assert.Equal("manager.new", response.Username);
        Assert.Equal("Manager", response.Role);
        Assert.Equal("Temp@12345", response.Password);
        Assert.Equal("Manager", repository.Users.Single().Role);
        Assert.Equal(1, repository.EnsureCanonicalRolesCallCount);
    }

    [Fact]
    public async Task CreateStaffAsync_WithDuplicateUsername_ThrowsConflict()
    {
        var repository = new FakeAdminUserRepository();
        repository.Seed(new AdminUserRecord
        {
            Id = Guid.NewGuid(),
            Username = "duplicate",
            Email = "old@test.com",
            FullName = "Dup",
            Role = "Employee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var service = new AdminUserService(repository, new FixedPasswordGenerator("Temp@12345"));

        await Assert.ThrowsAsync<AdminConflictException>(() =>
            service.CreateStaffAsync(new CreateStaffRequest("duplicate", "new@test.com", "New"), RoleNormalizer.Employee));
    }

    [Fact]
    public async Task UpdateUserAsync_WithAdminRole_ThrowsValidation()
    {
        var repository = new FakeAdminUserRepository();
        var adminId = Guid.NewGuid();
        repository.Seed(new AdminUserRecord
        {
            Id = adminId,
            Username = "admin",
            Email = "admin@test.com",
            FullName = "Admin",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var service = new AdminUserService(repository, new FixedPasswordGenerator("Temp@12345"));

        await Assert.ThrowsAsync<AdminValidationException>(() =>
            service.UpdateUserAsync(adminId, new UpdateStaffRequest("admin", "admin@test.com", "Admin", "Manager")));
    }

    [Fact]
    public async Task UpdateUserStatusAsync_ReactivatesUser()
    {
        var repository = new FakeAdminUserRepository();
        var userId = Guid.NewGuid();
        repository.Seed(new AdminUserRecord
        {
            Id = userId,
            Username = "employee",
            Email = "employee@test.com",
            FullName = "Employee",
            Role = "Employee",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var service = new AdminUserService(repository, new FixedPasswordGenerator("Temp@12345"));

        var response = await service.UpdateUserStatusAsync(userId, new UpdateStaffStatusRequest(true));

        Assert.True(response.IsActive);
        Assert.True(repository.Users.Single().IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesStoredPasswordHash()
    {
        var repository = new FakeAdminUserRepository();
        var userId = Guid.NewGuid();
        repository.Seed(new AdminUserRecord
        {
            Id = userId,
            Username = "employee",
            Email = "employee@test.com",
            FullName = "Employee",
            Role = "Employee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var service = new AdminUserService(repository, new FixedPasswordGenerator("Reset@1234"));

        var response = await service.ResetPasswordAsync(userId);

        Assert.Equal("Reset@1234", response.Password);
        Assert.Equal(PasswordHasher.Hash("Reset@1234"), repository.LastPasswordHash);
    }

    [Fact]
    public async Task GetUsersAsync_WithInvalidPage_ThrowsValidation()
    {
        var repository = new FakeAdminUserRepository();
        var service = new AdminUserService(repository, new FixedPasswordGenerator("Temp@12345"));

        await Assert.ThrowsAsync<AdminValidationException>(() =>
            service.GetUsersAsync(new UserListQuery { PageNumber = 0, PageSize = 10 }));
    }

    private sealed class FixedPasswordGenerator : IPasswordGenerator
    {
        private readonly string _password;

        public FixedPasswordGenerator(string password)
        {
            _password = password;
        }

        public string Generate() => _password;
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        public List<AdminUserRecord> Users { get; } = [];
        public int EnsureCanonicalRolesCallCount { get; private set; }
        public string? LastPasswordHash { get; private set; }

        public void Seed(AdminUserRecord user) => Users.Add(user);

        public Task EnsureCanonicalRolesAsync(CancellationToken cancellationToken = default)
        {
            EnsureCanonicalRolesCallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.Any(user => user.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && user.Id != excludeUserId));

        public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && user.Id != excludeUserId));

        public Task<AdminUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

        public Task<PagedResponse<StaffListItem>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
        {
            var items = Users
                .Select(user => new StaffListItem
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                })
                .ToList();

            return Task.FromResult(new PagedResponse<StaffListItem>
            {
                Items = items,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = items.Count,
                TotalPages = items.Count == 0 ? 0 : 1
            });
        }

        public Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string? fullName, string role, CancellationToken cancellationToken = default)
        {
            var user = new AdminUserRecord
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                FullName = fullName,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Users.Add(user);
            LastPasswordHash = passwordHash;
            return Task.FromResult(user.Id);
        }

        public Task<bool> UpdateUserAsync(Guid userId, string username, string email, string? fullName, string role, CancellationToken cancellationToken = default)
        {
            var current = Users.SingleOrDefault(user => user.Id == userId);
            if (current is null)
            {
                return Task.FromResult(false);
            }

            Users.Remove(current);
            Users.Add(new AdminUserRecord
            {
                Id = current.Id,
                Username = username,
                Email = email,
                FullName = fullName,
                Role = role,
                IsActive = current.IsActive,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(true);
        }

        public Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        {
            var current = Users.SingleOrDefault(user => user.Id == userId);
            if (current is null)
            {
                return Task.FromResult(false);
            }

            Users.Remove(current);
            Users.Add(new AdminUserRecord
            {
                Id = current.Id,
                Username = current.Username,
                Email = current.Email,
                FullName = current.FullName,
                Role = current.Role,
                IsActive = isActive,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(true);
        }

        public Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            LastPasswordHash = passwordHash;
            return Task.FromResult(Users.Any(user => user.Id == userId));
        }
    }
}

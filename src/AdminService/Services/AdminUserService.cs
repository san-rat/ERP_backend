using AdminService.Models;
using AdminService.Repositories;

namespace AdminService.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _userRepository;
    private readonly IPasswordGenerator _passwordGenerator;

    public AdminUserService(IAdminUserRepository userRepository, IPasswordGenerator passwordGenerator)
    {
        _userRepository = userRepository;
        _passwordGenerator = passwordGenerator;
    }

    public async Task<CreateStaffResponse> CreateStaffAsync(CreateStaffRequest request, string role, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);
        var normalizedRole = ValidateManagedRole(role);

        await _userRepository.EnsureCanonicalRolesAsync(cancellationToken);

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken: cancellationToken))
        {
            throw new AdminConflictException($"Username '{request.Username}' is already taken.");
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new AdminConflictException($"Email '{request.Email}' is already taken.");
        }

        var password = _passwordGenerator.Generate();
        var userId = await _userRepository.CreateUserAsync(
            request.Username.Trim(),
            request.Email.Trim(),
            PasswordHasher.Hash(password),
            string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            normalizedRole,
            cancellationToken);

        var createdUser = await GetManagedUserOrThrowAsync(userId, cancellationToken);
        return new CreateStaffResponse
        {
            UserId = createdUser.Id,
            Username = createdUser.Username,
            Email = createdUser.Email,
            FullName = createdUser.FullName,
            Role = createdUser.Role,
            IsActive = createdUser.IsActive,
            CreatedAt = createdUser.CreatedAt,
            UpdatedAt = createdUser.UpdatedAt,
            Password = password
        };
    }

    public async Task<PagedResponse<StaffListItem>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        if (query.PageNumber <= 0)
        {
            throw new AdminValidationException("PageNumber must be greater than 0.");
        }

        if (query.PageSize <= 0)
        {
            throw new AdminValidationException("PageSize must be greater than 0.");
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalized = RoleNormalizer.Normalize(query.Role);
            if (normalized is not (RoleNormalizer.Admin or RoleNormalizer.Manager or RoleNormalizer.Employee))
            {
                throw new AdminValidationException($"Role '{query.Role}' is not valid.");
            }
        }

        return await _userRepository.GetUsersAsync(query, cancellationToken);
    }

    public async Task<StaffListItem> UpdateUserAsync(Guid userId, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);
        var normalizedRole = ValidateManagedRole(request.Role);

        var currentUser = await GetManagedUserOrThrowAsync(userId, cancellationToken);

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim(), userId, cancellationToken))
        {
            throw new AdminConflictException($"Username '{request.Username}' is already taken.");
        }

        if (await _userRepository.EmailExistsAsync(request.Email.Trim(), userId, cancellationToken))
        {
            throw new AdminConflictException($"Email '{request.Email}' is already taken.");
        }

        var updated = await _userRepository.UpdateUserAsync(
            currentUser.Id,
            request.Username.Trim(),
            request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            normalizedRole,
            cancellationToken);

        if (!updated)
        {
            throw new AdminNotFoundException($"User '{userId}' was not found.");
        }

        return await MapManagedStaffAsync(userId, cancellationToken);
    }

    public async Task<StaffListItem> UpdateUserStatusAsync(Guid userId, UpdateStaffStatusRequest request, CancellationToken cancellationToken = default)
    {
        await GetManagedUserOrThrowAsync(userId, cancellationToken);

        var updated = await _userRepository.UpdateUserStatusAsync(userId, request.IsActive, cancellationToken);
        if (!updated)
        {
            throw new AdminNotFoundException($"User '{userId}' was not found.");
        }

        return await MapManagedStaffAsync(userId, cancellationToken);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetManagedUserOrThrowAsync(userId, cancellationToken);
        var password = _passwordGenerator.Generate();

        var updated = await _userRepository.UpdateUserPasswordAsync(userId, PasswordHasher.Hash(password), cancellationToken);
        if (!updated)
        {
            throw new AdminNotFoundException($"User '{userId}' was not found.");
        }

        return new ResetPasswordResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Password = password
        };
    }

    private async Task<AdminUserRecord> GetManagedUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AdminNotFoundException($"User '{userId}' was not found.");
        }

        if (RoleNormalizer.IsAdmin(user.Role))
        {
            throw new AdminValidationException("Admin accounts are read-only in AdminService.");
        }

        return user;
    }

    private async Task<StaffListItem> MapManagedStaffAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AdminNotFoundException($"User '{userId}' was not found.");
        }

        return new StaffListItem
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static void ValidateCreateRequest(CreateStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AdminValidationException("Username and Email are required.");
        }
    }

    private static void ValidateUpdateRequest(UpdateStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            throw new AdminValidationException("Username, Email, and Role are required.");
        }
    }

    private static string ValidateManagedRole(string role)
    {
        var normalizedRole = RoleNormalizer.Normalize(role);
        if (normalizedRole is not (RoleNormalizer.Manager or RoleNormalizer.Employee))
        {
            throw new AdminValidationException("Only Manager and Employee roles are supported by AdminService.");
        }

        return normalizedRole;
    }
}

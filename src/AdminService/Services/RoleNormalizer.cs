namespace AdminService.Services;

public static class RoleNormalizer
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static string Normalize(string? role)
    {
        return role?.Trim().ToUpperInvariant() switch
        {
            "ADMIN" => Admin,
            "MANAGER" => Manager,
            "USER" => Employee,
            "EMPLOYEE" => Employee,
            _ => role?.Trim() switch
            {
                Admin => Admin,
                Manager => Manager,
                Employee => Employee,
                _ => string.Empty
            }
        };
    }

    public static string ToCanonicalRole(string role)
    {
        return Normalize(role) switch
        {
            Admin => Admin,
            Manager => Manager,
            Employee => Employee,
            _ => throw new ArgumentException($"Unsupported role '{role}'.", nameof(role))
        };
    }

    public static bool IsAdmin(string? role) => Normalize(role) == Admin;
}

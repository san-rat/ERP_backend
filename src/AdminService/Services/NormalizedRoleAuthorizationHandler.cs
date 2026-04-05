using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Services;

public sealed class NormalizedRoleRequirement : IAuthorizationRequirement
{
    public NormalizedRoleRequirement(string requiredRole)
    {
        RequiredRole = requiredRole;
    }

    public string RequiredRole { get; }
}

public sealed class NormalizedRoleAuthorizationHandler : AuthorizationHandler<NormalizedRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NormalizedRoleRequirement requirement)
    {
        var roleClaims = context.User.Claims.Where(claim =>
            claim.Type == ClaimTypes.Role ||
            claim.Type == "role" ||
            claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase));

        if (roleClaims.Any(claim => RoleNormalizer.Normalize(claim.Value) == requirement.RequiredRole))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

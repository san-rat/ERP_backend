using System.Security.Claims;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Tests.Services;

public class NormalizedRoleAuthorizationHandlerTests
{
    [Theory]
    [InlineData("ADMIN")]
    [InlineData("Admin")]
    public async Task Handler_AcceptsLegacyAndCanonicalAdminClaims(string roleClaimValue)
    {
        var requirement = new NormalizedRoleRequirement(RoleNormalizer.Admin);
        var handler = new NormalizedRoleAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, roleClaimValue)
        ], "Test"));

        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}

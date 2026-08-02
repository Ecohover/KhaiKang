using System.Security.Claims;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class IdentityPrincipalFactory
{
    public ClaimsPrincipal Create(LoginSession session, AuthenticatedUserResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(IdentityConstants.SessionIdClaim, session.Id.ToString()),
        };
        claims.AddRange(user.SystemRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.SystemPermissions.Select(permission =>
            new Claim(PermissionCatalog.ClaimType, permission)));

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, IdentityConstants.AuthenticationScheme));
    }
}

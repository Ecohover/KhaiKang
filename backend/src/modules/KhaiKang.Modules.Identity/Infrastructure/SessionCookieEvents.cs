using KhaiKang.Modules.Identity.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class SessionCookieEvents(
    IdentityService identityService,
    IdentityPrincipalFactory principalFactory) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sessionIdValue = context.Principal?.FindFirst(IdentityConstants.SessionIdClaim)?.Value;
        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            context.RejectPrincipal();
            return;
        }

        var session = await identityService.GetValidSessionAsync(
            sessionId,
            context.HttpContext.RequestAborted);
        if (session is null)
        {
            context.RejectPrincipal();
            return;
        }

        var currentClaims = context.Principal!.Claims
            .Select(claim => (claim.Type, claim.Value))
            .Order()
            .ToArray();
        var refreshedPrincipal = principalFactory.Create(session.Value.Session, session.Value.User);
        var refreshedClaims = refreshedPrincipal.Claims
            .Select(claim => (claim.Type, claim.Value))
            .Order()
            .ToArray();
        if (!currentClaims.SequenceEqual(refreshedClaims))
        {
            context.ReplacePrincipal(refreshedPrincipal);
            context.ShouldRenew = true;
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}

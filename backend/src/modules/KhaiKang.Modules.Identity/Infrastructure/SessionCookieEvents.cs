using KhaiKang.Modules.Identity.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class SessionCookieEvents(IdentityService identityService) : CookieAuthenticationEvents
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

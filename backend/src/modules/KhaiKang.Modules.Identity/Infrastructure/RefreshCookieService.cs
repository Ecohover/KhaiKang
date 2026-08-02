using System.Security.Cryptography;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class RefreshCookieService(
    IDataProtectionProvider dataProtectionProvider,
    IHostEnvironment environment)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "KhaiKang.Identity.RefreshCookie.v1");

    public Guid? ReadSessionId(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(IdentityConstants.RefreshCookieName, out var protectedValue))
        {
            return null;
        }

        try
        {
            return Guid.Parse(_protector.Unprotect(protectedValue));
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException)
        {
            return null;
        }
    }

    public void Write(HttpResponse response, LoginSession session)
    {
        var options = CreateOptions();
        if (session.IsPersistent)
        {
            options.Expires = session.ExpiresAt;
        }

        response.Cookies.Append(
            IdentityConstants.RefreshCookieName,
            _protector.Protect(session.Id.ToString()),
            options);
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(IdentityConstants.RefreshCookieName, CreateOptions());
    }

    private CookieOptions CreateOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
        };
    }
}

using System.Threading.RateLimiting;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Endpoints;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KhaiKang.Modules.Identity.DependencyInjection;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<IdentityOptions>()
            .BindConfiguration(IdentityOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("KhaiKang")
            ?? throw new InvalidOperationException(
                "Connection string 'KhaiKang' is required.");

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<
            Microsoft.AspNetCore.Identity.IPasswordHasher<Account>,
            Microsoft.AspNetCore.Identity.PasswordHasher<Account>>();
        services.AddScoped<IdentityService>();
        services.AddScoped<RefreshCookieService>();
        services.AddScoped<SessionCookieEvents>();

        services.AddDataProtection()
            .SetApplicationName("KhaiKang");
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        var ticketMinutes = configuration.GetValue<int>(
            $"{IdentityOptions.SectionName}:AuthenticationTicketMinutes",
            30);

        services.AddAuthentication(IdentityConstants.AuthenticationScheme)
            .AddCookie(IdentityConstants.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "KhaiKang.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(ticketMinutes);
                options.SlidingExpiration = true;
                options.EventsType = typeof(SessionCookieEvents);
            });

        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapIdentityEndpoints();
    }
}

using System.Threading.RateLimiting;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Application.ResourceActors;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Controllers;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Endpoints;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication;
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
        services.AddScoped<AccountManagementService>();
        services.AddScoped<AiAccountService>();
        services.AddScoped<IAiTokenProtector, AiTokenProtector>();
        services.AddScoped<RefreshCookieService>();
        services.AddScoped<SessionCookieEvents>();
        services.AddScoped<IdentityPrincipalFactory>();
        services.AddScoped<IAccountDirectory, AccountDirectory>();
        services.AddScoped<IResourceActorDirectory, ResourceActorDirectory>();

        services.AddDataProtection()
            .SetApplicationName("KhaiKang");
        var requireSecureCookies = configuration.GetValue<bool?>(
            $"{IdentityOptions.SectionName}:RequireSecureCookies")
            ?? !environment.IsDevelopment();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = requireSecureCookies
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
        });

        var ticketMinutes = configuration.GetValue<int>(
            $"{IdentityOptions.SectionName}:AuthenticationTicketMinutes",
            30);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.AuthenticationSelectorScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.AuthenticationSelectorScheme;
            options.DefaultChallengeScheme = IdentityConstants.AuthenticationSelectorScheme;
            options.DefaultSignInScheme = IdentityConstants.AuthenticationScheme;
            options.DefaultSignOutScheme = IdentityConstants.AuthenticationScheme;
        })
            .AddPolicyScheme(
                IdentityConstants.AuthenticationSelectorScheme,
                displayName: null,
                options => options.ForwardDefaultSelector = context =>
                    context.Request.Headers.Authorization.ToString()
                        .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? IdentityConstants.AiTokenAuthenticationScheme
                        : IdentityConstants.AuthenticationScheme)
            .AddCookie(IdentityConstants.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "KhaiKang.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = requireSecureCookies
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(ticketMinutes);
                options.SlidingExpiration = true;
                options.EventsType = typeof(SessionCookieEvents);
            })
            .AddScheme<AuthenticationSchemeOptions, AiTokenAuthenticationHandler>(
                IdentityConstants.AiTokenAuthenticationScheme,
                _ => { });

        services.AddAuthorization(options =>
        {
            foreach (var permissionCode in PermissionCatalog.SystemPermissionCodes
                .Where(code => code != IdentityConstants.AiAccountCreatePermission))
            {
                options.AddPolicy(permissionCode, policy =>
                    policy.RequireClaim(PermissionCatalog.ClaimType, permissionCode));
            }


            options.AddPolicy(IdentityConstants.HumanSessionPolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(IdentityConstants.AccountTypeClaim, "human"));
            options.AddPolicy(IdentityConstants.AiAccountCreatePermission, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(IdentityConstants.AccountTypeClaim, "human")
                    .RequireAssertion(context =>
                        context.User.IsInRole(IdentityConstants.SystemAdminRole) ||
                        context.User.HasClaim(
                            PermissionCatalog.ClaimType,
                            IdentityConstants.AiAccountCreatePermission)));
        });
        services.AddControllers()
            .AddApplicationPart(typeof(AiAccountsController).Assembly);
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

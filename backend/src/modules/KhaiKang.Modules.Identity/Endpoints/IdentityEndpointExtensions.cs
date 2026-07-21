using System.Security.Claims;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.Identity.Endpoints;

public static class IdentityEndpointExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapSetupEndpoints(endpoints);
        MapAuthenticationEndpoints(endpoints);

        return endpoints;
    }

    private static void MapSetupEndpoints(IEndpointRouteBuilder endpoints)
    {
        var setup = endpoints.MapGroup("/api/v1/setup")
            .WithTags("Setup");

        setup.MapGet("/status", async (
            IdentityService identityService,
            CancellationToken cancellationToken) =>
        {
            var requiresInitialization = await identityService.RequiresInitializationAsync(
                cancellationToken);
            return Results.Ok(new SetupStatusResponse(requiresInitialization));
        })
        .WithName("GetSetupStatus")
        .Produces<SetupStatusResponse>();

        setup.MapPost("/initialize", async (
            IdentityService identityService,
            CancellationToken cancellationToken) =>
        {
            var response = await identityService.InitializeAdminAsync(cancellationToken);
            return response is null
                ? Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/setup/already-initialized",
                    "already_initialized",
                    "KhaiKang has already been initialized.")
                : Results.Ok(response);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("InitializeAdmin")
        .Produces<InitializeAdminResponse>()
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapAuthenticationEndpoints(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        auth.MapGet("/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new CsrfTokenResponse(tokens.RequestToken!));
        })
        .WithName("GetCsrfToken")
        .Produces<CsrfTokenResponse>();

        auth.MapPost("/login", async (
            LoginRequest request,
            IdentityService identityService,
            RefreshCookieService refreshCookieService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateLoginRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            var result = await identityService.LoginAsync(request, cancellationToken);
            if (result.Outcome != LoginOutcome.Succeeded ||
                result.Session is null ||
                result.User is null)
            {
                return Problem(
                    StatusCodes.Status401Unauthorized,
                    "https://khaikang.dev/problems/auth/invalid-credentials",
                    "invalid_credentials",
                    "The username or password is invalid.");
            }

            await SignInAsync(context, result.Session, result.User);
            refreshCookieService.Write(context.Response, result.Session);
            return Results.Ok(result.User);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .RequireRateLimiting("login")
        .WithName("Login")
        .Produces<AuthenticatedUserResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/refresh", async (
            IdentityService identityService,
            RefreshCookieService refreshCookieService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var sessionId = refreshCookieService.ReadSessionId(context.Request);
            var session = sessionId is null
                ? null
                : await identityService.GetValidSessionAsync(sessionId.Value, cancellationToken);
            if (session is null)
            {
                refreshCookieService.Delete(context.Response);
                return Problem(
                    StatusCodes.Status401Unauthorized,
                    "https://khaikang.dev/problems/auth/session-expired",
                    "session_expired",
                    "The login session has expired.");
            }

            await SignInAsync(context, session.Value.Session, session.Value.User);
            return Results.Ok(session.Value.User);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("RefreshSession")
        .Produces<AuthenticatedUserResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapGet("/me", async (
            IdentityService identityService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetSessionId(principal, out var sessionId))
            {
                return Results.Unauthorized();
            }

            var session = await identityService.GetValidSessionAsync(sessionId, cancellationToken);
            return session is null ? Results.Unauthorized() : Results.Ok(session.Value.User);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<AuthenticatedUserResponse>()
        .Produces(StatusCodes.Status401Unauthorized);

        auth.MapPost("/password", async (
            ChangePasswordRequest request,
            IdentityService identityService,
            ClaimsPrincipal principal,
            IOptions<IdentityOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetAccountId(principal, out var accountId) ||
                !TryGetSessionId(principal, out var sessionId))
            {
                return Results.Unauthorized();
            }

            var outcome = await identityService.ChangePasswordAsync(
                accountId,
                sessionId,
                request,
                cancellationToken);
            return outcome switch
            {
                ChangePasswordOutcome.Succeeded => Results.NoContent(),
                ChangePasswordOutcome.PasswordTooShort => Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [nameof(request.NewPassword)] =
                        [
                            $"Password must contain at least {options.Value.MinimumPasswordLength} characters.",
                        ],
                    }),
                ChangePasswordOutcome.InvalidCurrentPassword => Problem(
                    StatusCodes.Status400BadRequest,
                    "https://khaikang.dev/problems/auth/invalid-current-password",
                    "invalid_current_password",
                    "The current password is invalid."),
                _ => Results.Unauthorized(),
            };
        })
        .RequireAuthorization()
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("ChangePassword")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", async (
            IdentityService identityService,
            RefreshCookieService refreshCookieService,
            ClaimsPrincipal principal,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var sessionId = TryGetSessionId(principal, out var authenticatedSessionId)
                ? authenticatedSessionId
                : refreshCookieService.ReadSessionId(context.Request);
            if (sessionId is not null)
            {
                await identityService.RevokeSessionAsync(sessionId.Value, cancellationToken);
            }

            await context.SignOutAsync(IdentityConstants.AuthenticationScheme);
            refreshCookieService.Delete(context.Response);
            return Results.NoContent();
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("Logout")
        .Produces(StatusCodes.Status204NoContent);
    }

    private static IResult? ValidateLoginRequest(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static async Task SignInAsync(
        HttpContext context,
        LoginSession session,
        AuthenticatedUserResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(IdentityConstants.SessionIdClaim, session.Id.ToString()),
        };
        claims.AddRange(user.SystemRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, IdentityConstants.AuthenticationScheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = session.IsPersistent,
            ExpiresUtc = session.ExpiresAt,
        };

        await context.SignInAsync(
            IdentityConstants.AuthenticationScheme,
            principal,
            properties);
    }

    private static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId)
    {
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
    }

    private static bool TryGetSessionId(ClaimsPrincipal principal, out Guid sessionId)
    {
        return Guid.TryParse(principal.FindFirstValue(IdentityConstants.SessionIdClaim), out sessionId);
    }

    private static IResult Problem(int status, string type, string code, string detail)
    {
        return Results.Problem(
            statusCode: status,
            type: type,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}

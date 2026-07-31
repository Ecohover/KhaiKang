using System.Security.Claims;
using System.Text.RegularExpressions;
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
        MapAccountManagementEndpoints(endpoints);

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
            IdentityPrincipalFactory principalFactory,
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

            await SignInAsync(context, result.Session, result.User, principalFactory);
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
            IdentityPrincipalFactory principalFactory,
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

            await SignInAsync(
                context,
                session.Value.Session,
                session.Value.User,
                principalFactory);
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

    private static void MapAccountManagementEndpoints(IEndpointRouteBuilder endpoints)
    {
        var accounts = endpoints.MapGroup("/api/v1/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        accounts.MapGet("/", async (
            AccountManagementService accountManagementService,
            CancellationToken cancellationToken) =>
        {
            var response = await accountManagementService.ListAsync(cancellationToken);
            return Results.Ok(response);
        })
        .RequireAuthorization("account.read")
        .WithName("ListAccounts")
        .Produces<IReadOnlyList<AccountResponse>>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        accounts.MapPost("/", async (
            CreateAccountRequest request,
            AccountManagementService accountManagementService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateCreateAccountRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var actorAccountId))
            {
                return Results.Unauthorized();
            }

            var result = await accountManagementService.CreateAsync(
                actorAccountId,
                request,
                cancellationToken);
            return result.Outcome switch
            {
                CreateAccountOutcome.Succeeded => Results.Created(
                    $"/api/v1/accounts/{result.Response!.Account.Id}",
                    result.Response),
                CreateAccountOutcome.UsernameConflict => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/username-conflict",
                    "username_conflict",
                    "The username is already in use."),
                _ => Problem(
                    StatusCodes.Status500InternalServerError,
                    "https://khaikang.dev/problems/accounts/configuration-invalid",
                    "account_configuration_invalid",
                    "The default user role is not configured."),
            };
        })
        .RequireAuthorization("account.create")
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("CreateAccount")
        .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        accounts.MapPut("/{accountId:guid}", async (
            Guid accountId,
            UpdateAccountRequest request,
            AccountManagementService accountManagementService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateUpdateAccountRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var actorAccountId))
            {
                return Results.Unauthorized();
            }

            var result = await accountManagementService.UpdateAsync(
                actorAccountId,
                accountId,
                request,
                cancellationToken);
            return result.Outcome switch
            {
                UpdateAccountOutcome.Succeeded => Results.Ok(result.Account),
                UpdateAccountOutcome.NotFound => Problem(
                    StatusCodes.Status404NotFound,
                    "https://khaikang.dev/problems/accounts/not-found",
                    "account_not_found",
                    "The account does not exist."),
                UpdateAccountOutcome.UsernameConflict => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/username-conflict",
                    "username_conflict",
                    "The username is already in use."),
                UpdateAccountOutcome.CannotUpdateOwnAccount => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/cannot-update-own-account",
                    "cannot_update_own_account",
                    "You cannot update your own account from account management."),
                _ => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/version-conflict",
                    "account_version_conflict",
                    "The account was updated by another user."),
            };
        })
        .RequireAuthorization("account.update")
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("UpdateAccount")
        .Produces<AccountResponse>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        accounts.MapPut("/{accountId:guid}/status", async (
            Guid accountId,
            UpdateAccountStatusRequest request,
            AccountManagementService accountManagementService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateUpdateAccountStatusRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var actorAccountId))
            {
                return Results.Unauthorized();
            }

            var status = Enum.Parse<AccountStatus>(request.Status, true);
            var result = await accountManagementService.UpdateStatusAsync(
                actorAccountId,
                accountId,
                status,
                request.Version,
                cancellationToken);
            return result.Outcome switch
            {
                UpdateAccountStatusOutcome.Succeeded => Results.Ok(result.Account),
                UpdateAccountStatusOutcome.NotFound => Problem(
                    StatusCodes.Status404NotFound,
                    "https://khaikang.dev/problems/accounts/not-found",
                    "account_not_found",
                    "The account does not exist."),
                UpdateAccountStatusOutcome.CannotChangeOwnStatus => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/cannot-change-own-status",
                    "cannot_change_own_status",
                    "You cannot change the status of your own account."),
                _ => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/accounts/version-conflict",
                    "account_version_conflict",
                    "The account was updated by another user."),
            };
        })
        .RequireAuthorization("account.suspend")
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("UpdateAccountStatus")
        .Produces<AccountResponse>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
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

    private static IResult? ValidateCreateAccountRequest(CreateAccountRequest request)
    {
        var username = request.Username.Trim();
        if (username.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Username)] = ["Username is required."],
            });
        }

        if (username.Length > 100 ||
            !Regex.IsMatch(username, "^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$"))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Username)] =
                [
                    "Username must start with a letter or number and contain only letters, numbers, '.', '_' or '-'.",
                ],
            });
        }

        return null;
    }

    private static IResult? ValidateUpdateAccountStatusRequest(
        UpdateAccountStatusRequest request)
    {
        var validStatus = Enum.TryParse<AccountStatus>(request.Status, true, out _);
        var errors = new Dictionary<string, string[]>();
        if (!validStatus)
        {
            errors[nameof(request.Status)] =
                ["Status must be active, suspended or disabled."];
        }

        if (request.Version < 1)
        {
            errors[nameof(request.Version)] = ["Version must be greater than zero."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult? ValidateUpdateAccountRequest(UpdateAccountRequest request)
    {
        var username = request.Username.Trim();
        var errors = new Dictionary<string, string[]>();
        if (username.Length == 0)
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }
        else if (username.Length > 100 ||
            !Regex.IsMatch(username, "^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$"))
        {
            errors[nameof(request.Username)] =
            [
                "Username must start with a letter or number and contain only letters, numbers, '.', '_' or '-'.",
            ];
        }

        if (request.Version < 1)
        {
            errors[nameof(request.Version)] = ["Version must be greater than zero."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static async Task SignInAsync(
        HttpContext context,
        LoginSession session,
        AuthenticatedUserResponse user,
        IdentityPrincipalFactory principalFactory)
    {
        var principal = principalFactory.Create(session, user);
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

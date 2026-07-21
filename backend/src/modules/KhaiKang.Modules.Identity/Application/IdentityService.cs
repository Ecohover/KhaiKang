using System.Security.Cryptography;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectIdentityOptions = KhaiKang.Modules.Identity.Configuration.IdentityOptions;

namespace KhaiKang.Modules.Identity.Application;

public sealed class IdentityService(
    IdentityDbContext dbContext,
    IPasswordHasher<Account> passwordHasher,
    TimeProvider timeProvider,
    IOptions<ProjectIdentityOptions> options)
{
    private const string InitialPasswordCharacters =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%*-_";

    private readonly ProjectIdentityOptions _options = options.Value;

    public async Task<bool> RequiresInitializationAsync(CancellationToken cancellationToken)
    {
        return !await dbContext.Accounts.AnyAsync(cancellationToken);
    }

    public async Task<InitializeAdminResponse?> InitializeAdminAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var initialPassword = RandomNumberGenerator.GetString(InitialPasswordCharacters, 20);
        var account = new Account(Guid.NewGuid(), "admin", NormalizeUsername("admin"), now);
        account.SetInitialPassword(passwordHasher.HashPassword(account, initialPassword));

        var systemAdminRole = new SystemRole(
            Guid.NewGuid(),
            IdentityConstants.SystemAdminRole,
            IdentityConstants.SystemAdminRole.ToUpperInvariant());

        dbContext.Accounts.Add(account);
        dbContext.SystemRoles.Add(systemAdminRole);
        dbContext.AccountSystemRoles.Add(new AccountSystemRole(account.Id, systemAdminRole.Id));
        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            account.Id,
            "human",
            "admin_initialized",
            now,
            account.Id,
            "succeeded"));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return new InitializeAdminResponse(account.Username, initialPassword);
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var account = await dbContext.Accounts
            .Include(x => x.SystemRoles)
            .ThenInclude(x => x.SystemRole)
            .SingleOrDefaultAsync(
                x => x.NormalizedUsername == NormalizeUsername(request.Username),
                cancellationToken);

        if (account is null || account.Status != AccountStatus.Active)
        {
            await RecordLoginFailureAsync(account?.Id, now, cancellationToken);
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        var verification = passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordLoginFailureAsync(account.Id, now, cancellationToken);
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.SetInitialPassword(passwordHasher.HashPassword(account, request.Password));
        }

        var expiresAt = request.RememberMe
            ? now.AddDays(_options.RememberMeDays)
            : now.AddHours(_options.SessionHours);
        var session = new LoginSession(Guid.NewGuid(), account.Id, now, expiresAt, request.RememberMe);

        account.RecordSuccessfulLogin(now);
        dbContext.LoginSessions.Add(session);
        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            account.Id,
            "human",
            "login_succeeded",
            now,
            account.Id,
            "succeeded"));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResult(LoginOutcome.Succeeded, session, ToResponse(account));
    }

    public async Task<(LoginSession Session, AuthenticatedUserResponse User)?>
        GetValidSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var session = await dbContext.LoginSessions
            .Include(x => x.Account)
            .ThenInclude(x => x.SystemRoles)
            .ThenInclude(x => x.SystemRole)
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null ||
            !session.IsValidAt(now) ||
            session.Account.Status != AccountStatus.Active)
        {
            return null;
        }

        return (session, ToResponse(session.Account));
    }

    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid accountId,
        Guid currentSessionId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request.NewPassword.Length < _options.MinimumPasswordLength)
        {
            return ChangePasswordOutcome.PasswordTooShort;
        }

        var account = await dbContext.Accounts
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (account is null)
        {
            return ChangePasswordOutcome.SessionNotFound;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return ChangePasswordOutcome.InvalidCurrentPassword;
        }

        var now = timeProvider.GetUtcNow();
        account.ChangePassword(passwordHasher.HashPassword(account, request.NewPassword), account.Id, now);
        foreach (var session in account.Sessions.Where(x => x.Id != currentSessionId))
        {
            session.Revoke(now);
        }

        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            account.Id,
            "human",
            "password_changed",
            now,
            account.Id,
            "succeeded"));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ChangePasswordOutcome.Succeeded;
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.LoginSessions
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        session.Revoke(now);
        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            session.AccountId,
            "human",
            "logout",
            now,
            session.AccountId,
            "succeeded"));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordLoginFailureAsync(
        Guid? accountId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            accountId,
            accountId is null ? "anonymous" : "human",
            "login_failed",
            occurredAt,
            accountId,
            "failed"));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AuthenticatedUserResponse ToResponse(Account account)
    {
        return new AuthenticatedUserResponse(
            account.Id,
            account.Username,
            account.SystemRoles.Select(x => x.SystemRole.Name).Order().ToArray(),
            account.MustChangePassword);
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }
}

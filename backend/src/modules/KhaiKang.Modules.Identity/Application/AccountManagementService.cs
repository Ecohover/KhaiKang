using System.Security.Cryptography;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Application;

public sealed class AccountManagementService(
    IdentityDbContext dbContext,
    IPasswordHasher<Account> passwordHasher,
    TimeProvider timeProvider)
{
    private const string InitialPasswordCharacters =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%*-_";

    public async Task<IReadOnlyList<AccountResponse>> ListAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Include(account => account.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .OrderBy(account => account.Username)
            .Select(account => ToResponse(account))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CreateAccountResult> CreateAsync(
        Guid actorAccountId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.Accounts.AnyAsync(
            account => account.NormalizedUsername == normalizedUsername,
            cancellationToken))
        {
            return new CreateAccountResult(CreateAccountOutcome.UsernameConflict);
        }

        var userRole = await dbContext.SystemRoles
            .SingleOrDefaultAsync(
                role => role.NormalizedName == IdentityConstants.UserRole.ToUpperInvariant(),
                cancellationToken);
        if (userRole is null)
        {
            return new CreateAccountResult(CreateAccountOutcome.UserRoleNotConfigured);
        }

        var now = timeProvider.GetUtcNow();
        var initialPassword = RandomNumberGenerator.GetString(InitialPasswordCharacters, 20);
        var account = new Account(
            Guid.NewGuid(),
            username,
            normalizedUsername,
            now,
            actorAccountId);
        account.SetInitialPassword(passwordHasher.HashPassword(account, initialPassword));

        dbContext.Accounts.Add(account);
        dbContext.AccountSystemRoles.Add(new AccountSystemRole(account.Id, userRole.Id));
        dbContext.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            actorAccountId,
            "human",
            "account_created",
            now,
            account.Id,
            "succeeded"));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CreateAccountResult(CreateAccountOutcome.UsernameConflict);
        }

        return new CreateAccountResult(
            CreateAccountOutcome.Succeeded,
            new CreateAccountResponse(
                ToResponse(account, [IdentityConstants.UserRole]),
                initialPassword));
    }

    public async Task<UpdateAccountResult> UpdateAsync(
        Guid actorAccountId,
        Guid accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (actorAccountId == accountId)
        {
            return new UpdateAccountResult(UpdateAccountOutcome.CannotUpdateOwnAccount);
        }

        var account = await dbContext.Accounts
            .Include(item => item.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return new UpdateAccountResult(UpdateAccountOutcome.NotFound);
        }

        if (account.Version != request.Version)
        {
            return new UpdateAccountResult(UpdateAccountOutcome.VersionConflict);
        }

        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.Accounts.AnyAsync(
            item => item.Id != accountId && item.NormalizedUsername == normalizedUsername,
            cancellationToken))
        {
            return new UpdateAccountResult(UpdateAccountOutcome.UsernameConflict);
        }

        var previousUsername = account.Username;
        var now = timeProvider.GetUtcNow();
        account.Rename(username, normalizedUsername, actorAccountId, now);
        if (previousUsername != account.Username)
        {
            dbContext.AuditEvents.Add(new AuditEvent(
                Guid.NewGuid(),
                actorAccountId,
                "human",
                "account_updated",
                now,
                account.Id,
                "succeeded"));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateAccountResult(UpdateAccountOutcome.VersionConflict);
        }
        catch (DbUpdateException)
        {
            return new UpdateAccountResult(UpdateAccountOutcome.UsernameConflict);
        }

        return new UpdateAccountResult(
            UpdateAccountOutcome.Succeeded,
            ToResponse(account));
    }

    public async Task<UpdateAccountStatusResult> UpdateStatusAsync(
        Guid actorAccountId,
        Guid accountId,
        AccountStatus status,
        int version,
        CancellationToken cancellationToken)
    {
        if (actorAccountId == accountId)
        {
            return new UpdateAccountStatusResult(
                UpdateAccountStatusOutcome.CannotChangeOwnStatus);
        }

        var account = await dbContext.Accounts
            .Include(item => item.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .Include(item => item.Sessions)
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return new UpdateAccountStatusResult(UpdateAccountStatusOutcome.NotFound);
        }

        if (account.Version != version)
        {
            return new UpdateAccountStatusResult(UpdateAccountStatusOutcome.VersionConflict);
        }

        var previousStatus = account.Status;
        var now = timeProvider.GetUtcNow();
        account.ChangeStatus(status, actorAccountId, now);
        if (status != AccountStatus.Active)
        {
            foreach (var session in account.Sessions)
            {
                session.Revoke(now);
            }
        }

        if (previousStatus != status)
        {
            dbContext.AuditEvents.Add(new AuditEvent(
                Guid.NewGuid(),
                actorAccountId,
                "human",
                StatusEventType(status),
                now,
                account.Id,
                "succeeded"));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateAccountStatusResult(UpdateAccountStatusOutcome.VersionConflict);
        }

        return new UpdateAccountStatusResult(
            UpdateAccountStatusOutcome.Succeeded,
            ToResponse(account));
    }

    private static AccountResponse ToResponse(
        Account account,
        IReadOnlyList<string>? systemRoles = null)
    {
        return new AccountResponse(
            account.Id,
            account.Username,
            account.AccountType == AccountType.Human ? "human" : "ai_agent",
            account.Status.ToString().ToLowerInvariant(),
            systemRoles ?? account.SystemRoles
                .Select(mapping => mapping.SystemRole.Name)
                .Order()
                .ToArray(),
            account.MustChangePassword,
            account.LastLoginAt,
            account.CreatedAt,
            account.UpdatedAt,
            account.Version);
    }

    private static string StatusEventType(AccountStatus status)
    {
        return status switch
        {
            AccountStatus.Active => "account_restored",
            AccountStatus.Suspended => "account_suspended",
            AccountStatus.Disabled => "account_disabled",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
    }

    private static string NormalizeUsername(string username)
    {
        return username.ToUpperInvariant();
    }
}

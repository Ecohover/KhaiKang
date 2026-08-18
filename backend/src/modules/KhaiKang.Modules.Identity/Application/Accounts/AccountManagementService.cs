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
            return CreateAccountResult.Failure(CreateAccountOutcome.UsernameConflict);
        }

        var userRole = await dbContext.SystemRoles
            .SingleOrDefaultAsync(
                role => role.NormalizedName == IdentityConstants.UserRole.ToUpperInvariant(),
                cancellationToken);
        if (userRole is null)
        {
            return CreateAccountResult.Failure(CreateAccountOutcome.UserRoleNotConfigured);
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
        dbContext.AuditEvents.Add(AuditEvent.AccountCreated(actorAccountId, account.Id, now));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateAccountResult.Failure(CreateAccountOutcome.UsernameConflict);
        }

        return CreateAccountResult.Success(new CreateAccountResponse
        {
            Account = ToResponse(account, [IdentityConstants.UserRole]),
            InitialPassword = initialPassword,
        });
    }

    public async Task<UpdateAccountResult> UpdateAsync(
        Guid actorAccountId,
        Guid accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (actorAccountId == accountId)
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.CannotUpdateOwnAccount);
        }

        var account = await dbContext.Accounts
            .Include(item => item.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.NotFound);
        }

        if (account.Version != request.Version)
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.VersionConflict);
        }

        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.Accounts.AnyAsync(
            item => item.Id != accountId && item.NormalizedUsername == normalizedUsername,
            cancellationToken))
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.UsernameConflict);
        }

        var previousUsername = account.Username;
        var now = timeProvider.GetUtcNow();
        account.Rename(username, normalizedUsername, actorAccountId, now);
        if (previousUsername != account.Username)
        {
            dbContext.AuditEvents.Add(AuditEvent.AccountUpdated(actorAccountId, account.Id, now));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.VersionConflict);
        }
        catch (DbUpdateException)
        {
            return UpdateAccountResult.Failure(UpdateAccountOutcome.UsernameConflict);
        }

        return UpdateAccountResult.Success(ToResponse(account));
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
            return UpdateAccountStatusResult.Failure(
                UpdateAccountStatusOutcome.CannotChangeOwnStatus);
        }

        var account = await dbContext.Accounts
            .Include(item => item.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .Include(item => item.Sessions)
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return UpdateAccountStatusResult.Failure(UpdateAccountStatusOutcome.NotFound);
        }

        if (account.Version != version)
        {
            return UpdateAccountStatusResult.Failure(
                UpdateAccountStatusOutcome.VersionConflict);
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
            var auditEvent = status switch
            {
                AccountStatus.Active => AuditEvent.AccountRestored(actorAccountId, account.Id, now),
                AccountStatus.Suspended => AuditEvent.AccountSuspended(actorAccountId, account.Id, now),
                AccountStatus.Disabled => AuditEvent.AccountDisabled(actorAccountId, account.Id, now),
                _ => throw new ArgumentOutOfRangeException(nameof(status)),
            };
            dbContext.AuditEvents.Add(auditEvent);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateAccountStatusResult.Failure(
                UpdateAccountStatusOutcome.VersionConflict);
        }

        return UpdateAccountStatusResult.Success(ToResponse(account));
    }

    private static AccountResponse ToResponse(
        Account account,
        IReadOnlyList<string>? systemRoles = null)
    {
        return new AccountResponse
        {
            Id = account.Id,
            Username = account.Username,
            AccountType = account.AccountType == AccountType.Human ? "human" : "ai_agent",
            Status = account.Status.ToString().ToLowerInvariant(),
            SystemRoles = systemRoles ?? account.SystemRoles
                .Select(mapping => mapping.SystemRole.Name)
                .Order()
                .ToArray(),
            MustChangePassword = account.MustChangePassword,
            LastLoginAt = account.LastLoginAt,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Version = account.Version,
        };
    }

    private static string NormalizeUsername(string username)
    {
        return username.ToUpperInvariant();
    }
}

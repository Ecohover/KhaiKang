using System.Security.Cryptography;
using KhaiKang.Modules.Identity.Contracts.AiAccounts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed class AiAccountService(
    IdentityDbContext dbContext,
    IPasswordHasher<Account> passwordHasher,
    IAiTokenProtector tokenProtector,
    TimeProvider timeProvider)
{
    private const string InitialPasswordCharacters =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%*-_";

    public async Task<IReadOnlyList<AiAccountResponse>?> ListAsync(
        Guid actorAccountId,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(actorAccountId, cancellationToken);
        if (!IsActiveHuman(actor))
        {
            return null;
        }

        var query = dbContext.Accounts
            .AsNoTracking()
            .Include(account => account.OwnerAccount)
            .Include(account => account.AiToken)
            .Where(account => account.AccountType == AccountType.AiAgent);
        if (!IsAdministrator(actor!))
        {
            query = query.Where(account => account.OwnerAccountId == actorAccountId);
        }

        return await query
            .OrderBy(account => account.Username)
            .Select(account => ToResponse(account, account.OwnerAccount!))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AiAccountOperationResult> CreateAsync(
        CreateAiAccountCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        if (!IsActiveHuman(actor) || !CanCreateAiAccount(actor!))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.Forbidden);
        }

        var username = command.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        if (await dbContext.Accounts.AnyAsync(
            account => account.NormalizedUsername == normalizedUsername,
            cancellationToken))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.UsernameConflict);
        }

        var now = timeProvider.GetUtcNow();
        var initialPassword = RandomNumberGenerator.GetString(InitialPasswordCharacters, 20);
        var account = Account.CreateAi(new AiAccountCreation
        {
            Id = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = normalizedUsername,
            OwnerAccountId = actor!.Id,
            FixedRole = command.FixedRole,
            CreatedAt = now,
        });
        account.SetInitialPassword(passwordHasher.HashPassword(account, initialPassword));

        dbContext.Accounts.Add(account);
        dbContext.AuditEvents.Add(AuditEvent.AiAccountCreated(
            CreateAuditContext(account, actor, actor.Id, now)));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.UsernameConflict);
        }

        return AiAccountOperationResult.Success(
            ToResponse(account, actor),
            initialPassword);
    }

    public async Task<AiAccountOperationResult> SetApiEnabledAsync(
        SetAiApiEnabledCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        if (!IsActiveHuman(actor))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.Forbidden);
        }

        var account = await FindAiAccountAsync(command.AiAccountId, cancellationToken);
        if (account is null)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.NotFound);
        }

        if (!IsOwnerOrAdministrator(actor!, account))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.Forbidden);
        }

        if (account.Version != command.Version)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.VersionConflict);
        }

        var changed = account.AiApiEnabled != command.Enabled;
        var now = timeProvider.GetUtcNow();
        account.SetAiApiEnabled(command.Enabled, actor!.Id, now);
        if (changed)
        {
            var auditContext = CreateAuditContext(account, account.OwnerAccount!, actor.Id, now);
            dbContext.AuditEvents.Add(command.Enabled
                ? AuditEvent.AiApiEnabled(auditContext)
                : AuditEvent.AiApiDisabled(auditContext));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.VersionConflict);
        }

        return AiAccountOperationResult.Success(ToResponse(account, account.OwnerAccount!));
    }

    public async Task<AiTokenOperationResult> IssueTokenAsync(
        AiTokenCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        if (!IsActiveHuman(actor))
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.Forbidden);
        }

        var account = await FindAiAccountAsync(command.AiAccountId, cancellationToken);
        if (account is null)
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.NotFound);
        }

        if (account.OwnerAccountId != command.ActorAccountId)
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.Forbidden);
        }

        if (!account.AiApiEnabled)
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.AiApiDisabled);
        }

        var now = timeProvider.GetUtcNow();
        var existingToken = account.AiToken;
        var isRecreation = existingToken is not null;
        var issued = tokenProtector.Issue(existingToken?.Id ?? Guid.NewGuid());
        if (existingToken is null)
        {
            existingToken = new AiAccountToken(new AiAccountTokenCreation
            {
                Id = tokenProtector.ReadTokenId(issued.Plaintext)!.Value,
                AiAccountId = account.Id,
                Material = issued.Material,
                ActorAccountId = command.ActorAccountId,
                OccurredAt = now,
            });
            dbContext.AiAccountTokens.Add(existingToken);
        }
        else
        {
            existingToken.Recreate(issued.Material, command.ActorAccountId, now);
        }

        var auditContext = CreateAuditContext(
            account,
            account.OwnerAccount!,
            command.ActorAccountId,
            now);
        dbContext.AuditEvents.Add(isRecreation
            ? AuditEvent.AiTokenRecreated(auditContext)
            : AuditEvent.AiTokenCreated(auditContext));
        await dbContext.SaveChangesAsync(cancellationToken);

        return AiTokenOperationResult.Success(new AiTokenPlaintextResponse
        {
            Token = issued.Plaintext,
            CreatedAt = existingToken.UpdatedAt,
            Version = existingToken.Version,
        });
    }

    public async Task<AiTokenOperationResult> ViewTokenAsync(
        AiTokenCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        var account = await FindAiAccountAsync(command.AiAccountId, cancellationToken);
        if (!IsActiveHuman(actor) ||
            account is null ||
            account.OwnerAccountId != command.ActorAccountId)
        {
            return AiTokenOperationResult.Failure(
                account is null ? AiTokenOperationOutcome.NotFound : AiTokenOperationOutcome.Forbidden);
        }

        if (account.AiToken is null || !account.AiToken.IsActive)
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.TokenNotFound);
        }

        var plaintext = tokenProtector.Reveal(account.AiToken.ProtectedSecret);
        var now = timeProvider.GetUtcNow();
        dbContext.AuditEvents.Add(AuditEvent.AiTokenViewed(
            CreateAuditContext(account, account.OwnerAccount!, command.ActorAccountId, now)));
        await dbContext.SaveChangesAsync(cancellationToken);

        return AiTokenOperationResult.Success(new AiTokenPlaintextResponse
        {
            Token = plaintext,
            CreatedAt = account.AiToken.UpdatedAt,
            Version = account.AiToken.Version,
        });
    }

    public async Task<AiTokenOperationResult> RevokeTokenAsync(
        AiTokenCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        var account = await FindAiAccountAsync(command.AiAccountId, cancellationToken);
        if (!IsActiveHuman(actor) || account is null)
        {
            return AiTokenOperationResult.Failure(
                account is null ? AiTokenOperationOutcome.NotFound : AiTokenOperationOutcome.Forbidden);
        }

        if (!IsOwnerOrAdministrator(actor!, account))
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.Forbidden);
        }

        if (account.AiToken is null || !account.AiToken.IsActive)
        {
            return AiTokenOperationResult.Failure(AiTokenOperationOutcome.TokenNotFound);
        }

        var now = timeProvider.GetUtcNow();
        account.AiToken.Revoke(command.ActorAccountId, now);
        dbContext.AuditEvents.Add(AuditEvent.AiTokenRevoked(
            CreateAuditContext(account, account.OwnerAccount!, command.ActorAccountId, now)));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AiTokenOperationResult.Success();
    }

    public async Task<AiAccountOperationResult> TransferOwnerAsync(
        TransferAiAccountOwnerCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await FindActorAsync(command.ActorAccountId, cancellationToken);
        if (!IsActiveHuman(actor) || !IsAdministrator(actor!))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.Forbidden);
        }

        var account = await FindAiAccountAsync(command.AiAccountId, cancellationToken);
        if (account is null)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.NotFound);
        }

        if (account.Version != command.Version)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.VersionConflict);
        }

        var owner = await dbContext.Accounts.SingleOrDefaultAsync(
            item => item.Id == command.OwnerAccountId,
            cancellationToken);
        if (!IsActiveHuman(owner))
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.OwnerNotActiveHuman);
        }

        var now = timeProvider.GetUtcNow();
        account.TransferOwner(owner!.Id, actor!.Id, now);
        account.AiToken?.Revoke(actor.Id, now);
        dbContext.AuditEvents.Add(AuditEvent.AiAccountOwnerTransferred(
            CreateAuditContext(account, owner, actor.Id, now)));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AiAccountOperationResult.Failure(AiAccountOperationOutcome.VersionConflict);
        }

        return AiAccountOperationResult.Success(ToResponse(account, owner));
    }

    private async Task<Account?> FindActorAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .Include(account => account.SystemRoles)
            .ThenInclude(mapping => mapping.SystemRole)
            .ThenInclude(role => role.Permissions)
            .ThenInclude(mapping => mapping.Permission)
            .SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);
    }

    private async Task<Account?> FindAiAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .Include(account => account.OwnerAccount)
            .Include(account => account.AiToken)
            .SingleOrDefaultAsync(
                account => account.Id == accountId && account.AccountType == AccountType.AiAgent,
                cancellationToken);
    }

    private static bool IsActiveHuman(Account? account) =>
        account is { AccountType: AccountType.Human, Status: AccountStatus.Active };

    private static bool IsAdministrator(Account account) => account.SystemRoles.Any(
        mapping => mapping.SystemRole.Name == IdentityConstants.SystemAdminRole);

    private static bool CanCreateAiAccount(Account account) =>
        IsAdministrator(account) || account.SystemRoles.Any(mapping =>
            mapping.SystemRole.Permissions.Any(permission =>
                permission.Permission.Code == IdentityConstants.AiAccountCreatePermission));

    private static bool IsOwnerOrAdministrator(Account actor, Account aiAccount) =>
        aiAccount.OwnerAccountId == actor.Id || IsAdministrator(actor);

    private static AiAccountResponse ToResponse(Account account, Account owner) => new()
    {
        Id = account.Id,
        Username = account.Username,
        AccountType = "ai_agent",
        Status = account.Status.ToString().ToLowerInvariant(),
        OwnerAccountId = owner.Id,
        OwnerDisplayName = owner.Username,
        FixedRole = AiFixedRoleCodes.ToCode(account.FixedRole!.Value),
        AiApiEnabled = account.AiApiEnabled,
        HasActiveToken = account.AiToken?.IsActive == true,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt,
        Version = account.Version,
    };

    private static AiAuditEventContext CreateAuditContext(
        Account account,
        Account owner,
        Guid actorAccountId,
        DateTimeOffset occurredAt) => new()
    {
        ActorAccountId = actorAccountId,
        AiAccountId = account.Id,
        AiAccountName = account.Username,
        FixedRole = account.FixedRole!.Value,
        OwnerAccountId = owner.Id,
        OwnerDisplayName = owner.Username,
        OccurredAt = occurredAt,
    };
}

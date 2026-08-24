using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit.Sdk;
using ProjectIdentityOptions = KhaiKang.Modules.Identity.Configuration.IdentityOptions;

namespace KhaiKang.Api.IntegrationTests;

public sealed class AiIdentityPostgreSqlTests
{
    private const string ConnectionVariable = "KHAIKANG_IDENTITY_POSTGRES_TEST_CONNECTION";

    [Fact]
    [Trait("Database", "PostgreSQL")]
    public async Task TokenLifecycle_UsesPostgreSqlConstraintsAndAtomicOwnerTransfer()
    {
        var connectionString = RequireDisposableConnectionString();
        await RecreateDatabaseAsync(connectionString);
        var passwordHasher = new PasswordHasher<Account>();
        var tokenProtector = CreateTokenProtector();

        Guid administratorId;
        Guid newOwnerId;
        Guid aiAccountId;
        string plaintext;
        await using (var dbContext = CreateDbContext(connectionString))
        {
            var identityService = new IdentityService(
                dbContext,
                passwordHasher,
                TimeProvider.System,
                Options.Create(new ProjectIdentityOptions()));
            Assert.NotNull(await identityService.InitializeAdminAsync(CancellationToken.None));
            administratorId = await dbContext.Accounts
                .Where(account => account.NormalizedUsername == "ADMIN")
                .Select(account => account.Id)
                .SingleAsync();

            var now = TimeProvider.System.GetUtcNow();
            var newOwner = new Account(
                Guid.NewGuid(),
                "token-owner",
                "TOKEN-OWNER",
                now,
                administratorId);
            newOwner.SetInitialPassword(passwordHasher.HashPassword(newOwner, "Temporary!Password1"));
            dbContext.Accounts.Add(newOwner);
            await dbContext.SaveChangesAsync();
            newOwnerId = newOwner.Id;

            var service = CreateService(dbContext, passwordHasher, tokenProtector);
            var created = await service.CreateAsync(new CreateAiAccountCommand
            {
                ActorAccountId = administratorId,
                Username = "ai-postgres-token",
                FixedRole = AiFixedRole.DevelopmentAi,
            }, CancellationToken.None);
            Assert.Equal(AiAccountOperationOutcome.Succeeded, created.Outcome);
            aiAccountId = created.Account!.Id;

            var enabled = await service.SetApiEnabledAsync(new SetAiApiEnabledCommand
            {
                ActorAccountId = administratorId,
                AiAccountId = aiAccountId,
                Enabled = true,
                Version = created.Account.Version,
            }, CancellationToken.None);
            Assert.Equal(AiAccountOperationOutcome.Succeeded, enabled.Outcome);

            var issued = await service.IssueTokenAsync(new AiTokenCommand
            {
                ActorAccountId = administratorId,
                AiAccountId = aiAccountId,
            }, CancellationToken.None);
            Assert.Equal(AiTokenOperationOutcome.Succeeded, issued.Outcome);
            plaintext = issued.Token!.Token;

            var transferred = await service.TransferOwnerAsync(new TransferAiAccountOwnerCommand
            {
                ActorAccountId = administratorId,
                AiAccountId = aiAccountId,
                OwnerAccountId = newOwnerId,
                Version = enabled.Account!.Version,
            }, CancellationToken.None);
            Assert.Equal(AiAccountOperationOutcome.Succeeded, transferred.Outcome);
        }

        await using (var duplicateContext = CreateDbContext(connectionString))
        {
            var duplicateIssued = tokenProtector.Issue(Guid.NewGuid());
            duplicateContext.AiAccountTokens.Add(new AiAccountToken(new AiAccountTokenCreation
            {
                Id = tokenProtector.ReadTokenId(duplicateIssued.Plaintext)!.Value,
                AiAccountId = aiAccountId,
                Material = duplicateIssued.Material,
                ActorAccountId = newOwnerId,
                OccurredAt = TimeProvider.System.GetUtcNow(),
            }));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateContext.SaveChangesAsync());
            Assert.True(ContainsPostgresUniqueViolation(exception));
        }

        await using var verification = CreateDbContext(connectionString);
        var account = await verification.Accounts.SingleAsync(item => item.Id == aiAccountId);
        var token = await verification.AiAccountTokens.SingleAsync(item => item.AiAccountId == aiAccountId);
        Assert.Equal(newOwnerId, account.OwnerAccountId);
        Assert.Equal(AiFixedRole.DevelopmentAi, account.FixedRole);
        Assert.True(account.AiApiEnabled);
        Assert.False(token.IsActive);
        Assert.DoesNotContain(plaintext, token.ProtectedSecret, StringComparison.Ordinal);
        Assert.DoesNotContain(plaintext, token.LookupVerifier, StringComparison.Ordinal);
        Assert.Equal(64, token.LookupVerifier.Length);

        var transferAudit = await verification.AuditEvents.SingleAsync(audit =>
            audit.EventType == "ai_account_owner_transferred" && audit.TargetId == aiAccountId);
        Assert.Equal("ai-postgres-token", transferAudit.TargetNameSnapshot);
        Assert.Equal(AiFixedRole.DevelopmentAi, transferAudit.TargetFixedRoleSnapshot);
        Assert.Equal(newOwnerId, transferAudit.OwnerAccountIdSnapshot);
        Assert.Equal("token-owner", transferAudit.OwnerDisplayNameSnapshot);
    }

    private static AiAccountService CreateService(
        IdentityDbContext dbContext,
        IPasswordHasher<Account> passwordHasher,
        IAiTokenProtector tokenProtector) => new(
            dbContext,
            passwordHasher,
            tokenProtector,
            TimeProvider.System);

    private static IAiTokenProtector CreateTokenProtector() => new AiTokenProtector(
        new EphemeralDataProtectionProvider(),
        Options.Create(new ProjectIdentityOptions()));

    private static IdentityDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new IdentityDbContext(options);
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        await using var dbContext = CreateDbContext(connectionString);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    private static string RequireDisposableConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw SkipException.ForSkip(
                $"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Database is null ||
            !builder.Database.StartsWith("khaikang_identity_ai_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The PostgreSQL test database name must start with 'khaikang_identity_ai_'; actual '{builder.Database}'.");
        }

        return connectionString;
    }

    private static bool ContainsPostgresUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }
}

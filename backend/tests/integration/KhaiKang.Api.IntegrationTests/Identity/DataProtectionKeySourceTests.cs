using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectIdentityOptions = KhaiKang.Modules.Identity.Configuration.IdentityOptions;

namespace KhaiKang.Api.IntegrationTests.Identity;

public sealed class DataProtectionKeySourceTests
{
    [Fact]
    public void ProductionStartup_RejectsMissingPersistentDataProtectionDirectory()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting(
                    "ConnectionStrings:KhaiKang",
                    "Host=localhost;Database=startup_validation;Username=unused;Password=unused");
                builder.UseSetting("DataProtection:KeysDirectory", string.Empty);
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("DataProtection:KeysDirectory", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersistentKeyRing_AllowsOwnerToRecoverTokenAfterProviderRestart()
    {
        var keysDirectory = Path.Combine(
            Path.GetTempPath(),
            "khaikang-data-protection-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(keysDirectory);

        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;
            var passwordHasher = new PasswordHasher<Account>();
            Guid ownerId;
            Guid aiAccountId;
            string lookupVerifier;

            await using (var setupContext = new IdentityDbContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync();
                var identityService = new IdentityService(
                    setupContext,
                    passwordHasher,
                    TimeProvider.System,
                    Options.Create(new ProjectIdentityOptions()));
                Assert.NotNull(await identityService.InitializeAdminAsync(CancellationToken.None));
                ownerId = await setupContext.Accounts
                    .Where(account => account.NormalizedUsername == "ADMIN")
                    .Select(account => account.Id)
                    .SingleAsync();

                var service = CreateService(
                    setupContext,
                    passwordHasher,
                    CreateTokenProtector(keysDirectory));
                var created = await service.CreateAsync(new CreateAiAccountCommand
                {
                    ActorAccountId = ownerId,
                    Username = "ai-key-continuity",
                    FixedRole = AiFixedRole.DevelopmentAi,
                }, CancellationToken.None);
                Assert.Equal(AiAccountOperationOutcome.Succeeded, created.Outcome);
                aiAccountId = created.Account!.Id;

                var enabled = await service.SetApiEnabledAsync(new SetAiApiEnabledCommand
                {
                    ActorAccountId = ownerId,
                    AiAccountId = aiAccountId,
                    Enabled = true,
                    Version = created.Account.Version,
                }, CancellationToken.None);
                Assert.Equal(AiAccountOperationOutcome.Succeeded, enabled.Outcome);

                var issued = await service.IssueTokenAsync(new AiTokenCommand
                {
                    ActorAccountId = ownerId,
                    AiAccountId = aiAccountId,
                }, CancellationToken.None);
                Assert.Equal(AiTokenOperationOutcome.Succeeded, issued.Outcome);
                lookupVerifier = await setupContext.AiAccountTokens
                    .Where(token => token.AiAccountId == aiAccountId)
                    .Select(token => token.LookupVerifier)
                    .SingleAsync();
            }

            await using var restartedContext = new IdentityDbContext(options);
            var restartedProtector = CreateTokenProtector(keysDirectory);
            var restartedService = CreateService(
                restartedContext,
                passwordHasher,
                restartedProtector);
            var recovered = await restartedService.ViewTokenAsync(new AiTokenCommand
            {
                ActorAccountId = ownerId,
                AiAccountId = aiAccountId,
            }, CancellationToken.None);

            Assert.Equal(AiTokenOperationOutcome.Succeeded, recovered.Outcome);
            Assert.True(restartedProtector.Verify(recovered.Token!.Token, lookupVerifier));
        }
        finally
        {
            Directory.Delete(keysDirectory, recursive: true);
        }
    }

    private static AiAccountService CreateService(
        IdentityDbContext dbContext,
        IPasswordHasher<Account> passwordHasher,
        IAiTokenProtector tokenProtector) => new(
            dbContext,
            passwordHasher,
            tokenProtector,
            TimeProvider.System);

    private static IAiTokenProtector CreateTokenProtector(string keysDirectory)
    {
        var provider = DataProtectionProvider.Create(
            new DirectoryInfo(keysDirectory),
            builder => builder.SetApplicationName("KhaiKang"));
        return new AiTokenProtector(provider, Options.Create(new ProjectIdentityOptions()));
    }
}

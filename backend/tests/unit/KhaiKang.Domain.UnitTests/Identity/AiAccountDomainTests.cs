using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Domain.UnitTests.Identity;

public sealed class AiAccountDomainTests
{
    [Fact]
    public void CreateAi_PinsOwnerAndFixedRoleWithApiDisabled()
    {
        var ownerId = Guid.NewGuid();
        var account = Account.CreateAi(new AiAccountCreation
        {
            Id = Guid.NewGuid(),
            Username = "ai-development",
            NormalizedUsername = "AI-DEVELOPMENT",
            OwnerAccountId = ownerId,
            FixedRole = AiFixedRole.DevelopmentAi,
            CreatedAt = DateTimeOffset.Parse("2026-08-24T00:00:00Z"),
        });

        Assert.Equal(AccountType.AiAgent, account.AccountType);
        Assert.Equal(ownerId, account.OwnerAccountId);
        Assert.Equal(AiFixedRole.DevelopmentAi, account.FixedRole);
        Assert.False(account.AiApiEnabled);
        Assert.False(typeof(Account).GetProperty(nameof(Account.FixedRole))!.SetMethod!.IsPublic);
    }

    [Fact]
    public void SetAiApiEnabled_RejectsHumanAccount()
    {
        var account = new Account(
            Guid.NewGuid(),
            "human",
            "HUMAN",
            DateTimeOffset.Parse("2026-08-24T00:00:00Z"));

        var exception = Assert.Throws<InvalidOperationException>(() => account.SetAiApiEnabled(
            true,
            account.Id,
            DateTimeOffset.Parse("2026-08-24T00:01:00Z")));

        Assert.Equal("Only AI accounts can enable the AI API.", exception.Message);
    }

    [Fact]
    public void Token_RecreateReusesRecordAndRevokeMarksItInactive()
    {
        var actorId = Guid.NewGuid();
        var token = new AiAccountToken(new AiAccountTokenCreation
        {
            Id = Guid.NewGuid(),
            AiAccountId = Guid.NewGuid(),
            Material = CreateMaterial("first"),
            ActorAccountId = actorId,
            OccurredAt = DateTimeOffset.Parse("2026-08-24T00:00:00Z"),
        });

        token.Recreate(
            CreateMaterial("second"),
            actorId,
            DateTimeOffset.Parse("2026-08-24T00:01:00Z"));
        token.Revoke(actorId, DateTimeOffset.Parse("2026-08-24T00:02:00Z"));

        Assert.Equal("protected-second", token.ProtectedSecret);
        Assert.Equal(new string('b', 64), token.LookupVerifier);
        Assert.False(token.IsActive);
        Assert.Equal(3, token.Version);
    }

    private static AiTokenMaterial CreateMaterial(string suffix) => new()
    {
        ProtectedSecret = $"protected-{suffix}",
        LookupVerifier = suffix == "first" ? new string('a', 64) : new string('b', 64),
        ProtectionVersion = "v1",
    };
}

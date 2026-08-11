using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class LoginSessionTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesLifetimeAndPersistenceChoice()
    {
        var accountId = Guid.NewGuid();
        var expiresAt = CreatedAt.AddHours(8);

        var session = new LoginSession(
            Guid.NewGuid(), accountId, CreatedAt, expiresAt, isPersistent: true);

        Assert.Equal(accountId, session.AccountId);
        Assert.Equal(CreatedAt, session.CreatedAt);
        Assert.Equal(expiresAt, session.ExpiresAt);
        Assert.True(session.IsPersistent);
        Assert.Null(session.RevokedAt);
    }

    [Fact]
    public void IsValidAt_BeforeExpiryReturnsTrue()
    {
        var session = CreateSession();

        var isValid = session.IsValidAt(CreatedAt.AddMinutes(30));

        Assert.True(isValid);
    }

    [Fact]
    public void IsValidAt_AtOrAfterExpiryReturnsFalse()
    {
        var expiresAt = CreatedAt.AddHours(1);
        var session = CreateSession(expiresAt);

        Assert.False(session.IsValidAt(expiresAt));
        Assert.False(session.IsValidAt(expiresAt.AddMinutes(1)));
    }

    [Fact]
    public void Revoke_InvalidatesSessionAndPreservesFirstRevocationTime()
    {
        var session = CreateSession();
        var revokedAt = CreatedAt.AddMinutes(15);

        session.Revoke(revokedAt);
        session.Revoke(revokedAt.AddMinutes(5));

        Assert.Equal(revokedAt, session.RevokedAt);
        Assert.False(session.IsValidAt(revokedAt));
    }

    private static LoginSession CreateSession(DateTimeOffset? expiresAt = null)
    {
        return new LoginSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt,
            expiresAt ?? CreatedAt.AddHours(1),
            isPersistent: false);
    }
}

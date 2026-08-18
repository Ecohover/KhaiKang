using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class AccountTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 24, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveHumanAccountThatMustChangePassword()
    {
        var creatorId = Guid.NewGuid();

        var account = new Account(
            Guid.NewGuid(),
            "reviewer.one",
            "REVIEWER.ONE",
            CreatedAt,
            creatorId);

        Assert.Equal("reviewer.one", account.Username);
        Assert.Equal("REVIEWER.ONE", account.NormalizedUsername);
        Assert.Equal(AccountType.Human, account.AccountType);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.True(account.MustChangePassword);
        Assert.Equal(creatorId, account.CreatedByAccountId);
        Assert.Equal(creatorId, account.UpdatedByAccountId);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void Rename_ChangesUsernameAndAuditMetadata()
    {
        var account = CreateAccount();
        var actorId = Guid.NewGuid();
        var occurredAt = CreatedAt.AddMinutes(5);

        account.Rename("reviewer.two", "REVIEWER.TWO", actorId, occurredAt);

        Assert.Equal("reviewer.two", account.Username);
        Assert.Equal("REVIEWER.TWO", account.NormalizedUsername);
        Assert.Equal(actorId, account.UpdatedByAccountId);
        Assert.Equal(occurredAt, account.UpdatedAt);
        Assert.Equal(2, account.Version);
    }

    [Fact]
    public void Rename_WhenValueIsUnchanged_DoesNotIncreaseVersion()
    {
        var account = CreateAccount();

        account.Rename(
            account.Username,
            account.NormalizedUsername,
            Guid.NewGuid(),
            CreatedAt.AddMinutes(5));

        Assert.Equal(1, account.Version);
        Assert.Equal(CreatedAt, account.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WhenStatusChanges_UpdatesAuditMetadataAndVersion()
    {
        var account = CreateAccount();
        var actorId = Guid.NewGuid();
        var occurredAt = CreatedAt.AddMinutes(5);

        account.ChangeStatus(AccountStatus.Suspended, actorId, occurredAt);

        Assert.Equal(AccountStatus.Suspended, account.Status);
        Assert.Equal(actorId, account.UpdatedByAccountId);
        Assert.Equal(occurredAt, account.UpdatedAt);
        Assert.Equal(2, account.Version);
    }

    [Fact]
    public void ChangeStatus_WhenStatusIsUnchanged_DoesNotIncreaseVersion()
    {
        var account = CreateAccount();

        account.ChangeStatus(AccountStatus.Active, Guid.NewGuid(), CreatedAt.AddMinutes(5));

        Assert.Equal(1, account.Version);
        Assert.Equal(CreatedAt, account.UpdatedAt);
    }

    [Fact]
    public void ChangePassword_ClearsInitialPasswordRequirement()
    {
        var account = CreateAccount();
        var actorId = Guid.NewGuid();
        var occurredAt = CreatedAt.AddMinutes(5);

        account.ChangePassword("new-password-hash", actorId, occurredAt);

        Assert.Equal("new-password-hash", account.PasswordHash);
        Assert.False(account.MustChangePassword);
        Assert.Equal(actorId, account.UpdatedByAccountId);
        Assert.Equal(2, account.Version);
    }

    [Fact]
    public void SetInitialPassword_StoresHashWithoutChangingAuditVersion()
    {
        var account = CreateAccount();

        account.SetInitialPassword("initial-password-hash");

        Assert.Equal("initial-password-hash", account.PasswordHash);
        Assert.True(account.MustChangePassword);
        Assert.Equal(CreatedAt, account.UpdatedAt);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void RecordSuccessfulLogin_CapturesLoginTimeAndUpdatesVersion()
    {
        var account = CreateAccount();
        var loggedInAt = CreatedAt.AddMinutes(10);

        account.RecordSuccessfulLogin(loggedInAt);

        Assert.Equal(loggedInAt, account.LastLoginAt);
        Assert.Equal(loggedInAt, account.UpdatedAt);
        Assert.Equal(2, account.Version);
    }

    private static Account CreateAccount()
    {
        return new Account(
            Guid.NewGuid(),
            "reviewer.one",
            "REVIEWER.ONE",
            CreatedAt,
            Guid.NewGuid());
    }
}

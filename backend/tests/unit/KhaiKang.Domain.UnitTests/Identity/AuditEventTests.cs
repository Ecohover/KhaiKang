using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Domain.UnitTests.Identity;

public sealed class AuditEventTests
{
    [Fact]
    public void SuccessfulAccountEvents_PreserveTheirAuditContract()
    {
        var actorId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-08-11T12:00:00+08:00");
        (string EventType, AuditEvent AuditEvent)[] events =
        [
            ("account_created", AuditEvent.AccountCreated(actorId, accountId, occurredAt)),
            ("account_updated", AuditEvent.AccountUpdated(actorId, accountId, occurredAt)),
            ("account_restored", AuditEvent.AccountRestored(actorId, accountId, occurredAt)),
            ("account_suspended", AuditEvent.AccountSuspended(actorId, accountId, occurredAt)),
            ("account_disabled", AuditEvent.AccountDisabled(actorId, accountId, occurredAt)),
        ];

        foreach (var item in events)
        {
            Assert.NotEqual(Guid.Empty, item.AuditEvent.Id);
            Assert.Equal(actorId, item.AuditEvent.ActorId);
            Assert.Equal("human", item.AuditEvent.ActorType);
            Assert.Equal(item.EventType, item.AuditEvent.EventType);
            Assert.Equal(occurredAt, item.AuditEvent.OccurredAt);
            Assert.Equal(accountId, item.AuditEvent.TargetId);
            Assert.Equal("succeeded", item.AuditEvent.Outcome);
        }
    }

    [Fact]
    public void SelfServiceEvents_UseTheAccountAsActorAndTarget()
    {
        var accountId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-08-11T12:00:00+08:00");
        (string EventType, AuditEvent AuditEvent)[] events =
        [
            ("admin_initialized", AuditEvent.AdminInitialized(accountId, occurredAt)),
            ("login_succeeded", AuditEvent.LoginSucceeded(accountId, occurredAt)),
            ("password_changed", AuditEvent.PasswordChanged(accountId, occurredAt)),
            ("logout", AuditEvent.Logout(accountId, occurredAt)),
        ];

        foreach (var item in events)
        {
            Assert.NotEqual(Guid.Empty, item.AuditEvent.Id);
            Assert.Equal(accountId, item.AuditEvent.ActorId);
            Assert.Equal("human", item.AuditEvent.ActorType);
            Assert.Equal(item.EventType, item.AuditEvent.EventType);
            Assert.Equal(occurredAt, item.AuditEvent.OccurredAt);
            Assert.Equal(accountId, item.AuditEvent.TargetId);
            Assert.Equal("succeeded", item.AuditEvent.Outcome);
        }
    }

    [Fact]
    public void LoginFailed_WithKnownAccount_RecordsHumanFailure()
    {
        var accountId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-08-11T12:00:00+08:00");

        var auditEvent = AuditEvent.LoginFailed(accountId, occurredAt);

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(accountId, auditEvent.ActorId);
        Assert.Equal("human", auditEvent.ActorType);
        Assert.Equal("login_failed", auditEvent.EventType);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
        Assert.Equal(accountId, auditEvent.TargetId);
        Assert.Equal("failed", auditEvent.Outcome);
    }

    [Fact]
    public void LoginFailed_WithoutKnownAccount_RecordsAnonymousFailure()
    {
        var occurredAt = DateTimeOffset.Parse("2026-08-11T12:00:00+08:00");

        var auditEvent = AuditEvent.LoginFailed(null, occurredAt);

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Null(auditEvent.ActorId);
        Assert.Equal("anonymous", auditEvent.ActorType);
        Assert.Equal("login_failed", auditEvent.EventType);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
        Assert.Null(auditEvent.TargetId);
        Assert.Equal("failed", auditEvent.Outcome);
    }
}

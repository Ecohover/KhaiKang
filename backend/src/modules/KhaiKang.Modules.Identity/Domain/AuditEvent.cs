namespace KhaiKang.Modules.Identity.Domain;

public sealed class AuditEvent
{
    private const string AnonymousActorType = "anonymous";
    private const string HumanActorType = "human";
    private const string SucceededOutcome = "succeeded";
    private const string FailedOutcome = "failed";
    private const string AccountCreatedEventType = "account_created";
    private const string AccountUpdatedEventType = "account_updated";
    private const string AccountRestoredEventType = "account_restored";
    private const string AccountSuspendedEventType = "account_suspended";
    private const string AccountDisabledEventType = "account_disabled";
    private const string AdminInitializedEventType = "admin_initialized";
    private const string LoginSucceededEventType = "login_succeeded";
    private const string LoginFailedEventType = "login_failed";
    private const string PasswordChangedEventType = "password_changed";
    private const string LogoutEventType = "logout";

    private AuditEvent()
    {
    }

    public static AuditEvent AccountCreated(
        Guid actorId,
        Guid accountId,
        DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AccountCreatedEventType,
            actorId: actorId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent AccountUpdated(
        Guid actorId,
        Guid accountId,
        DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AccountUpdatedEventType,
            actorId: actorId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent AccountRestored(
        Guid actorId,
        Guid accountId,
        DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AccountRestoredEventType,
            actorId: actorId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent AccountSuspended(
        Guid actorId,
        Guid accountId,
        DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AccountSuspendedEventType,
            actorId: actorId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent AccountDisabled(
        Guid actorId,
        Guid accountId,
        DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AccountDisabledEventType,
            actorId: actorId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent AdminInitialized(Guid accountId, DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: AdminInitializedEventType,
            actorId: accountId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent LoginSucceeded(Guid accountId, DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: LoginSucceededEventType,
            actorId: accountId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent LoginFailed(Guid? accountId, DateTimeOffset occurredAt)
    {
        var actorType = accountId is null ? AnonymousActorType : HumanActorType;
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorId = accountId,
            ActorType = actorType,
            EventType = LoginFailedEventType,
            OccurredAt = occurredAt,
            TargetId = accountId,
            Outcome = FailedOutcome,
        };
    }

    public static AuditEvent PasswordChanged(Guid accountId, DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: PasswordChangedEventType,
            actorId: accountId,
            targetId: accountId,
            occurredAt: occurredAt);

    public static AuditEvent Logout(Guid accountId, DateTimeOffset occurredAt) =>
        Succeeded(
            eventType: LogoutEventType,
            actorId: accountId,
            targetId: accountId,
            occurredAt: occurredAt);

    public Guid Id { get; private set; }

    public Guid? ActorId { get; private set; }

    public string ActorType { get; private set; } = null!;

    public string EventType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? TargetId { get; private set; }

    public string Outcome { get; private set; } = null!;

    private static AuditEvent Succeeded(
        string eventType,
        Guid actorId,
        Guid targetId,
        DateTimeOffset occurredAt)
    {
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorType = HumanActorType,
            EventType = eventType,
            OccurredAt = occurredAt,
            TargetId = targetId,
            Outcome = SucceededOutcome,
        };
    }
}

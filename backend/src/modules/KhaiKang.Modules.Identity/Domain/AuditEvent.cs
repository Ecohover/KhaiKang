namespace KhaiKang.Modules.Identity.Domain;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        Guid id,
        Guid? actorId,
        string actorType,
        string eventType,
        DateTimeOffset occurredAt,
        Guid? targetId,
        string outcome)
    {
        Id = id;
        ActorId = actorId;
        ActorType = actorType;
        EventType = eventType;
        OccurredAt = occurredAt;
        TargetId = targetId;
        Outcome = outcome;
    }

    public Guid Id { get; private set; }

    public Guid? ActorId { get; private set; }

    public string ActorType { get; private set; } = null!;

    public string EventType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? TargetId { get; private set; }

    public string Outcome { get; private set; } = null!;
}

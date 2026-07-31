namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectAuditEvent
{
    private ProjectAuditEvent()
    {
    }

    public ProjectAuditEvent(
        Guid id,
        Guid actorId,
        string eventType,
        DateTimeOffset occurredAt,
        Guid targetId)
    {
        Id = id;
        ActorId = actorId;
        EventType = eventType;
        OccurredAt = occurredAt;
        TargetId = targetId;
    }

    public Guid Id { get; private set; }

    public Guid ActorId { get; private set; }

    public string ActorType { get; private set; } = "human";

    public string EventType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid TargetId { get; private set; }

    public string Outcome { get; private set; } = "succeeded";
}

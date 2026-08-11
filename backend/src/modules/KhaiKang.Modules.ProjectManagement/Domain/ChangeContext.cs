namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ChangeContext
{
    public ChangeContext(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        ActorAccountId = actorAccountId;
        OccurredAt = occurredAt;
    }

    public Guid ActorAccountId { get; }

    public DateTimeOffset OccurredAt { get; }
}

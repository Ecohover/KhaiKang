namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ChangeContext
{
    public ChangeContext(
        Guid actorAccountId,
        DateTimeOffset occurredAt,
        OperationActorSnapshot? actorSnapshot = null)
    {
        ActorAccountId = actorAccountId;
        OccurredAt = occurredAt;
        ActorType = actorSnapshot?.ActorType ?? "human";
        ActorNameSnapshot = actorSnapshot?.ActorName;
        ActorFixedRoleSnapshot = actorSnapshot?.ActorFixedRole;
        OwnerAccountIdSnapshot = actorSnapshot?.OwnerAccountId;
        OwnerDisplayNameSnapshot = actorSnapshot?.OwnerDisplayName;
    }

    public Guid ActorAccountId { get; }

    public DateTimeOffset OccurredAt { get; }

    public string ActorType { get; }

    public string? ActorNameSnapshot { get; }

    public string? ActorFixedRoleSnapshot { get; }

    public Guid? OwnerAccountIdSnapshot { get; }

    public string? OwnerDisplayNameSnapshot { get; }
}

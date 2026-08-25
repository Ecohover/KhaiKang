namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestOperationEvent
{
    private TestOperationEvent() { }

    public TestOperationEvent(TestOperationCreation creation)
    {
        Id = Guid.NewGuid();
        ResourceId = creation.ResourceId;
        ResourceType = creation.ResourceType;
        EventType = creation.EventType;
        ActorId = creation.ActorId;
        ActorType = creation.ActorType;
        OccurredAt = creation.OccurredAt;
        ActorNameSnapshot = creation.ActorNameSnapshot;
        ActorFixedRoleSnapshot = creation.ActorFixedRoleSnapshot;
        OwnerAccountIdSnapshot = creation.OwnerAccountIdSnapshot;
        OwnerDisplayNameSnapshot = creation.OwnerDisplayNameSnapshot;
    }

    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public Guid ActorId { get; private set; }
    public string ActorType { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public string? ActorNameSnapshot { get; private set; }
    public string? ActorFixedRoleSnapshot { get; private set; }
    public Guid? OwnerAccountIdSnapshot { get; private set; }
    public string? OwnerDisplayNameSnapshot { get; private set; }
}

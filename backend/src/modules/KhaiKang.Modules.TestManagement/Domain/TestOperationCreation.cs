namespace KhaiKang.Modules.TestManagement.Domain;

public sealed record TestOperationCreation
{
    public required Guid ResourceId { get; init; }
    public required string ResourceType { get; init; }
    public required string EventType { get; init; }
    public required Guid ActorId { get; init; }
    public required string ActorType { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? ActorNameSnapshot { get; init; }
    public string? ActorFixedRoleSnapshot { get; init; }
    public Guid? OwnerAccountIdSnapshot { get; init; }
    public string? OwnerDisplayNameSnapshot { get; init; }
}

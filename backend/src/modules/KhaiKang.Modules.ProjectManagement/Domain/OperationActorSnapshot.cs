namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed record OperationActorSnapshot
{
    public required string ActorType { get; init; }
    public string? ActorName { get; init; }
    public string? ActorFixedRole { get; init; }
    public Guid? OwnerAccountId { get; init; }
    public string? OwnerDisplayName { get; init; }
}

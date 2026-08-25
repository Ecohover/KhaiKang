namespace KhaiKang.Modules.Identity.Contracts;

public sealed record ResourceOperationResponse
{
    public required Guid Id { get; init; }
    public required string EventType { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required string ActorType { get; init; }
    public string? AiAccountName { get; init; }
    public string? FixedRole { get; init; }
    public Guid? OwnerAccountId { get; init; }
    public string? OwnerDisplayName { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}

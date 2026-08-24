namespace KhaiKang.Modules.Identity.Domain;

public sealed record AiAuditEventContext
{
    public required Guid ActorAccountId { get; init; }
    public required Guid AiAccountId { get; init; }
    public required string AiAccountName { get; init; }
    public required AiFixedRole FixedRole { get; init; }
    public required Guid OwnerAccountId { get; init; }
    public required string OwnerDisplayName { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}

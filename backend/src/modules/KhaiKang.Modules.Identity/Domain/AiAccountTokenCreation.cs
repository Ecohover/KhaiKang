namespace KhaiKang.Modules.Identity.Domain;

public sealed record AiAccountTokenCreation
{
    public required Guid Id { get; init; }
    public required Guid AiAccountId { get; init; }
    public required AiTokenMaterial Material { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}

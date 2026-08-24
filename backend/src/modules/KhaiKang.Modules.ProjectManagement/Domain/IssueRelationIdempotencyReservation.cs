namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed record IssueRelationIdempotencyReservation
{
    public required Guid Id { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string RequestFingerprint { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

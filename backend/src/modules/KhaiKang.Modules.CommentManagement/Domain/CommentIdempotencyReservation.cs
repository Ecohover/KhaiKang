namespace KhaiKang.Modules.CommentManagement.Domain;

public sealed record CommentIdempotencyReservation
{
    public required Guid Id { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required string OperationCode { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string RequestFingerprint { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

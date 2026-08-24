namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TraceabilityIdempotencyRecord
{
    private TraceabilityIdempotencyRecord() { }
    internal TraceabilityIdempotencyRecord(Guid id, Guid actorId, string key, string fingerprint, DateTimeOffset now)
    {
        Id = id; ActorAccountId = actorId; IdempotencyKey = key; RequestFingerprint = fingerprint;
        State = "incomplete_transient_failure"; CreatedAt = now;
    }
    public Guid Id { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string? ResponseSnapshot { get; private set; }
    public string? RejectionSnapshot { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public void CompleteSuccess(string snapshot, DateTimeOffset now) { ResponseSnapshot = snapshot; State = "terminal_success"; CompletedAt = now; }
    public void CompleteRejection(string snapshot, DateTimeOffset now) { RejectionSnapshot = snapshot; State = "terminal_business_rejection"; CompletedAt = now; }
}

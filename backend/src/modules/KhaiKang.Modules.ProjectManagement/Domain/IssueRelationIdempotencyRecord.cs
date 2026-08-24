namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelationIdempotencyRecord
{
    private IssueRelationIdempotencyRecord() { }

    private IssueRelationIdempotencyRecord(IssueRelationIdempotencyReservation reservation)
    {
        Id = reservation.Id;
        ActorAccountId = reservation.ActorAccountId;
        IdempotencyKey = reservation.IdempotencyKey;
        RequestFingerprint = reservation.RequestFingerprint;
        State = IssueRelationIdempotencyState.IncompleteTransientFailure;
        CreatedAt = reservation.CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public IssueRelationIdempotencyState State { get; private set; }
    public string? ResponseSnapshot { get; private set; }
    public string? RejectionSnapshot { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static IssueRelationIdempotencyRecord Reserve(
        IssueRelationIdempotencyReservation reservation) => new(reservation);

    public void CompleteSuccess(string responseSnapshot, DateTimeOffset completedAt)
    {
        EnsureIncomplete();
        ResponseSnapshot = responseSnapshot;
        State = IssueRelationIdempotencyState.TerminalSuccess;
        CompletedAt = completedAt;
    }

    public void CompleteBusinessRejection(string rejectionSnapshot, DateTimeOffset completedAt)
    {
        EnsureIncomplete();
        RejectionSnapshot = rejectionSnapshot;
        State = IssueRelationIdempotencyState.TerminalBusinessRejection;
        CompletedAt = completedAt;
    }

    private void EnsureIncomplete()
    {
        if (State != IssueRelationIdempotencyState.IncompleteTransientFailure)
        {
            throw new InvalidOperationException("Only incomplete relation idempotency records can become terminal.");
        }
    }
}

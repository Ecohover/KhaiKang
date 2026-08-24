namespace KhaiKang.Modules.CommentManagement.Domain;

public sealed class CommentIdempotencyRecord
{
    private CommentIdempotencyRecord()
    {
    }

    private CommentIdempotencyRecord(CommentIdempotencyReservation reservation)
    {
        Id = reservation.Id;
        ActorAccountId = reservation.ActorAccountId;
        OperationCode = reservation.OperationCode;
        IdempotencyKey = reservation.IdempotencyKey;
        RequestFingerprint = reservation.RequestFingerprint;
        State = CommentIdempotencyState.IncompleteTransientFailure;
        CreatedAt = reservation.CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public string OperationCode { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public CommentIdempotencyState State { get; private set; }
    public string? ResponseSnapshot { get; private set; }
    public string? RejectionSnapshot { get; private set; }
    public Guid? TargetId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static CommentIdempotencyRecord Reserve(CommentIdempotencyReservation reservation) =>
        new(reservation);

    public void CompleteSuccess(
        Guid targetId,
        string responseSnapshot,
        DateTimeOffset completedAt)
    {
        EnsureIncomplete();
        TargetId = targetId;
        ResponseSnapshot = responseSnapshot;
        State = CommentIdempotencyState.TerminalSuccess;
        CompletedAt = completedAt;
    }

    public void CompleteBusinessRejection(
        string rejectionSnapshot,
        DateTimeOffset completedAt)
    {
        EnsureIncomplete();
        RejectionSnapshot = rejectionSnapshot;
        State = CommentIdempotencyState.TerminalBusinessRejection;
        CompletedAt = completedAt;
    }

    private void EnsureIncomplete()
    {
        if (State != CommentIdempotencyState.IncompleteTransientFailure)
        {
            throw new InvalidOperationException("Only incomplete idempotency records can transition to a terminal state.");
        }
    }
}

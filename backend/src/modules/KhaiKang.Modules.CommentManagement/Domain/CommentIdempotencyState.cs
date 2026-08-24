namespace KhaiKang.Modules.CommentManagement.Domain;

public enum CommentIdempotencyState
{
    IncompleteTransientFailure,
    TerminalSuccess,
    TerminalBusinessRejection,
}

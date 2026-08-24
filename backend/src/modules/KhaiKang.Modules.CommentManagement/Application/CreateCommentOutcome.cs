namespace KhaiKang.Modules.CommentManagement.Application;

public enum CreateCommentOutcome
{
    Created,
    ValidationRejected,
    NotFound,
    IdempotencyConflict,
}

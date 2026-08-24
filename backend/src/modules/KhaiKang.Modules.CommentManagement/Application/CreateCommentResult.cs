using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Modules.CommentManagement.Application;

public sealed record CreateCommentResult
{
    private CreateCommentResult(
        CreateCommentOutcome outcome,
        CommentResponse? comment,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Outcome = outcome;
        Comment = comment;
        ValidationErrors = validationErrors;
    }

    public CreateCommentOutcome Outcome { get; }
    public CommentResponse? Comment { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static CreateCommentResult Created(CommentResponse comment) =>
        new(CreateCommentOutcome.Created, comment, null);

    public static CreateCommentResult Rejected(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(CreateCommentOutcome.ValidationRejected, null, validationErrors);

    public static CreateCommentResult NotFound() =>
        new(CreateCommentOutcome.NotFound, null, null);

    public static CreateCommentResult IdempotencyConflict() =>
        new(CreateCommentOutcome.IdempotencyConflict, null, null);
}

namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CreateCommentRequest
{
    public required string Content { get; init; }
}

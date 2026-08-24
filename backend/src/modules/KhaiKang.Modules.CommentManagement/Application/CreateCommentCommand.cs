using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Modules.CommentManagement.Application;

public sealed record CreateCommentCommand
{
    public required CommentResourceType ResourceType { get; init; }
    public required Guid ResourceId { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string Content { get; init; }
}

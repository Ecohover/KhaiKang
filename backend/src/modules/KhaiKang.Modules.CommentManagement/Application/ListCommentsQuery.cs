using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Modules.CommentManagement.Application;

public sealed record ListCommentsQuery
{
    public required CommentResourceType ResourceType { get; init; }
    public required Guid ResourceId { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required int Page { get; init; }
}

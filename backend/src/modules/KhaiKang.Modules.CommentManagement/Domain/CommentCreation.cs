using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Modules.CommentManagement.Domain;

public sealed record CommentCreation
{
    public required Guid Id { get; init; }
    public required CommentResourceType ResourceType { get; init; }
    public required Guid ResourceId { get; init; }
    public required CommentActorSnapshot Actor { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

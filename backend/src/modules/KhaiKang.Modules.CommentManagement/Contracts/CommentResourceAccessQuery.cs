using KhaiKang.Modules.Identity.Contracts;

namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CommentResourceAccessQuery
{
    public required CommentResourceType ResourceType { get; init; }

    public required Guid ResourceId { get; init; }

    public required ResourceActorSnapshot Actor { get; init; }
}

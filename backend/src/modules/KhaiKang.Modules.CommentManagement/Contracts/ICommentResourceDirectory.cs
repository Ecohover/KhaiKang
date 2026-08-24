namespace KhaiKang.Modules.CommentManagement.Contracts;

public interface ICommentResourceDirectory
{
    bool Supports(CommentResourceType resourceType);

    Task<CommentResourceAccess> GetAccessAsync(
        CommentResourceType resourceType,
        Guid resourceId,
        Guid accountId,
        CancellationToken cancellationToken);
}

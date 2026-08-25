namespace KhaiKang.Modules.CommentManagement.Contracts;

public interface ICommentResourceDirectory
{
    bool Supports(CommentResourceType resourceType);

    Task<CommentResourceAccess> GetAccessAsync(
        CommentResourceAccessQuery query,
        CancellationToken cancellationToken);
}

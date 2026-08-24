namespace KhaiKang.Modules.CommentManagement.Contracts;

public interface ICommentActorDirectory
{
    Task<CommentActorSnapshot?> FindActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}

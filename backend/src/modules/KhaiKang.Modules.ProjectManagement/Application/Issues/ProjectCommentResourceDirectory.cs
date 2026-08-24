using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application.Issues;

public sealed class ProjectCommentResourceDirectory(IIssueDirectory issueDirectory) : ICommentResourceDirectory
{
    public bool Supports(CommentResourceType resourceType) => resourceType == CommentResourceType.Issue;

    public async Task<CommentResourceAccess> GetAccessAsync(CommentResourceType resourceType, Guid resourceId, Guid accountId, CancellationToken cancellationToken)
    {
        if (!Supports(resourceType)) return Denied();
        var readable = await issueDirectory.FindReadableAsync(resourceId, accountId, cancellationToken);
        if (readable is null) return Denied();
        var commentable = await issueDirectory.FindCommentableAsync(resourceId, accountId, cancellationToken);
        return new CommentResourceAccess
        {
            CanRead = true,
            CanCreate = commentable is not null,
        };
    }

    private static CommentResourceAccess Denied() => new()
    {
        CanRead = false,
        CanCreate = false,
    };
}

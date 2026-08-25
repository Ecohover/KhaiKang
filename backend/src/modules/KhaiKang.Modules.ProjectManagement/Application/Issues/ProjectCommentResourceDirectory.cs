using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application.Issues;

public sealed class ProjectCommentResourceDirectory(
    IIssueDirectory issueDirectory,
    ProjectCommentTargetAdmission targetAdmission) : ICommentResourceDirectory
{
    public bool Supports(CommentResourceType resourceType) => resourceType == CommentResourceType.Issue;

    public async Task<CommentResourceAccess> GetAccessAsync(
        CommentResourceAccessQuery query,
        CancellationToken cancellationToken)
    {
        if (!Supports(query.ResourceType)) return Denied();
        var readable = await issueDirectory.FindReadableAsync(
            query.ResourceId,
            query.Actor.ScopeAccountId,
            cancellationToken);
        if (readable is null) return Denied();
        var commentable = await issueDirectory.FindCommentableAsync(
            query.ResourceId,
            query.Actor.ScopeAccountId,
            cancellationToken);
        var canCreate = commentable is not null &&
            (!query.Actor.IsAi ||
                await targetAdmission.CanCreateAsync(
                    query.ResourceId,
                    query.Actor,
                    cancellationToken));
        return new CommentResourceAccess
        {
            CanRead = true,
            CanCreate = canCreate,
        };
    }

    private static CommentResourceAccess Denied() => new()
    {
        CanRead = false,
        CanCreate = false,
    };
}

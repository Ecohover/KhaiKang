using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.TestManagement.Application;

internal sealed class TestCommentTargetAdmission(
    IIssueCommentContextDirectory issueCommentContextDirectory)
{
    private const string QaAiRole = "qa_ai";

    public async Task<bool> CanCreateAsync(
        CommentResourceAccessQuery query,
        TestCommentContext? context,
        CancellationToken cancellationToken)
    {
        if (query.Actor.FixedRole != QaAiRole)
        {
            return false;
        }

        if (context is null)
        {
            return false;
        }

        return await issueCommentContextDirectory.IsAssignedTestIssueAsync(
            new AssignedTestIssueCommentContextQuery
            {
                IssueId = context.TestIssueId,
                ExpectedProjectId = context.ProjectId,
                AiActorAccountId = query.Actor.AccountId,
                OwnerScopeAccountId = query.Actor.ScopeAccountId,
            },
            cancellationToken);
    }
}

using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application.Issues;

public sealed class ProjectCommentTargetAdmission(
    ProjectManagementDbContext dbContext,
    IResourceActorDirectory resourceActorDirectory,
    IIssueCommentContextDirectory issueCommentContextDirectory)
{
    private const string PmAiRole = "pm_ai";
    private const string DevelopmentAiRole = "development_ai";
    private const string QaAiRole = "qa_ai";
    private const string ReviewerAiRole = "reviewer_ai";
    private const string DevOpsAiRole = "devops_ai";

    public async Task<bool> CanCreateAsync(
        Guid issueId,
        ResourceActorSnapshot actor,
        CancellationToken cancellationToken)
    {
        var issue = await dbContext.Issues
            .AsNoTracking()
            .Include(item => item.IssueType)
            .SingleOrDefaultAsync(item => item.Id == issueId, cancellationToken);
        if (issue is null)
        {
            return false;
        }

        return actor.FixedRole switch
        {
            PmAiRole => issue.IssueType.Code is "story" or "task",
            DevelopmentAiRole =>
                issue.IssueType.Code == "task" &&
                issue.AssigneeAccountId == actor.AccountId,
            QaAiRole => await IsAssignedTestIssueAsync(issue, actor, cancellationToken),
            ReviewerAiRole => await IsDevelopmentTaskAsync(issue, cancellationToken),
            DevOpsAiRole => issue.IssueType.Code == "task",
            _ => false,
        };
    }

    private Task<bool> IsAssignedTestIssueAsync(
        Issue issue,
        ResourceActorSnapshot actor,
        CancellationToken cancellationToken) =>
        issueCommentContextDirectory.IsAssignedTestIssueAsync(
            new AssignedTestIssueCommentContextQuery
            {
                IssueId = issue.Id,
                ExpectedProjectId = issue.ProjectId,
                AiActorAccountId = actor.AccountId,
                OwnerScopeAccountId = actor.ScopeAccountId,
            },
            cancellationToken);

    private async Task<bool> IsDevelopmentTaskAsync(
        Issue issue,
        CancellationToken cancellationToken)
    {
        if (issue.IssueType.Code != "task" || issue.AssigneeAccountId is not { } assigneeId)
        {
            return false;
        }

        var assignee = await resourceActorDirectory.FindActiveAsync(
            assigneeId,
            cancellationToken);
        return assignee is { IsAi: true, FixedRole: DevelopmentAiRole };
    }
}

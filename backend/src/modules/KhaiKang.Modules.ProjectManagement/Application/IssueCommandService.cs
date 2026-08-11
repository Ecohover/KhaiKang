using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueCommandService(
    IssueService issueService,
    IIssueDirectory issueDirectory) : IIssueCommandService
{
    public async Task<IssueCommandResult> CreateAsync(
        Guid projectId,
        Guid accountId,
        IssueCommandRequest request,
        CancellationToken cancellationToken)
    {
        var result = await issueService.CreateAsync(
            projectId,
            accountId,
            new CreateIssueRequest(
                request.Title,
                request.TypeCode,
                request.PriorityCode,
                request.Description,
                null,
                null,
                request.AssigneeAccountId),
            cancellationToken);

        if (result.Outcome != IssueMutationOutcome.Succeeded || result.Issue is null)
        {
            return new(Map(result.Outcome));
        }

        var issue = await issueDirectory.FindReadableAsync(
            result.Issue.Id,
            accountId,
            cancellationToken);
        return issue is null
            ? new(IssueCommandOutcome.NotFound)
            : new(IssueCommandOutcome.Succeeded, issue);
    }

    private static IssueCommandOutcome Map(IssueMutationOutcome outcome) => outcome switch
    {
        IssueMutationOutcome.NotFound => IssueCommandOutcome.NotFound,
        IssueMutationOutcome.Forbidden => IssueCommandOutcome.Forbidden,
        IssueMutationOutcome.InvalidOption => IssueCommandOutcome.InvalidOption,
        IssueMutationOutcome.InvalidAssignee => IssueCommandOutcome.InvalidAssignee,
        IssueMutationOutcome.ProjectInactive => IssueCommandOutcome.ProjectInactive,
        IssueMutationOutcome.NumberConflict => IssueCommandOutcome.Conflict,
        IssueMutationOutcome.VersionConflict => IssueCommandOutcome.Conflict,
        _ => IssueCommandOutcome.Conflict,
    };
}

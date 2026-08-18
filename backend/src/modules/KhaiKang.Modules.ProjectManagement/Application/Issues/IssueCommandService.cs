using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueCommandService(
    IssueService issueService,
    IIssueDirectory issueDirectory) : IIssueCommandService
{
    public async Task<CreateIssueCommandResult> CreateAsync(
        Guid projectId,
        Guid accountId,
        CreateIssueCommand command,
        CancellationToken cancellationToken)
    {
        var result = await issueService.CreateAsync(
            projectId,
            accountId,
            new CreateIssueRequest(
                title: command.Title,
                typeCode: command.TypeCode)
            {
                PriorityCode = command.PriorityCode,
                Description = command.Description,
                AssigneeAccountId = command.AssigneeAccountId,
            },
            cancellationToken);

        if (result.Outcome != IssueMutationOutcome.Succeeded || result.Issue is null)
        {
            return CreateIssueCommandResult.Failure(Map(result.Outcome));
        }

        var issue = await issueDirectory.FindReadableAsync(
            result.Issue.Id,
            accountId,
            cancellationToken);
        return issue is null
            ? CreateIssueCommandResult.Failure(CreateIssueCommandOutcome.NotFound)
            : CreateIssueCommandResult.Success(issue);
    }

    private static CreateIssueCommandOutcome Map(IssueMutationOutcome outcome) => outcome switch
    {
        IssueMutationOutcome.NotFound => CreateIssueCommandOutcome.NotFound,
        IssueMutationOutcome.Forbidden => CreateIssueCommandOutcome.Forbidden,
        IssueMutationOutcome.InvalidOption => CreateIssueCommandOutcome.InvalidOption,
        IssueMutationOutcome.InvalidAssignee => CreateIssueCommandOutcome.InvalidAssignee,
        IssueMutationOutcome.ProjectInactive => CreateIssueCommandOutcome.ProjectInactive,
        IssueMutationOutcome.NumberConflict => CreateIssueCommandOutcome.Conflict,
        IssueMutationOutcome.VersionConflict => CreateIssueCommandOutcome.Conflict,
        _ => CreateIssueCommandOutcome.Conflict,
    };
}

namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record CreateIssueCommandResult
{
    private CreateIssueCommandResult(
        CreateIssueCommandOutcome outcome,
        IssueDirectoryEntry? issue)
    {
        Outcome = outcome;
        Issue = issue;
    }

    public CreateIssueCommandOutcome Outcome { get; }

    public IssueDirectoryEntry? Issue { get; }

    public static CreateIssueCommandResult Success(IssueDirectoryEntry issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return new CreateIssueCommandResult(CreateIssueCommandOutcome.Succeeded, issue);
    }

    public static CreateIssueCommandResult Failure(CreateIssueCommandOutcome outcome)
    {
        if (outcome == CreateIssueCommandOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failed Issue command cannot use the succeeded outcome.");
        }

        return new CreateIssueCommandResult(outcome, issue: null);
    }
}

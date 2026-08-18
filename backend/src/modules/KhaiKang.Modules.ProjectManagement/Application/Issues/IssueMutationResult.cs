using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record IssueMutationResult
{
    private IssueMutationResult(
        IssueMutationOutcome outcome,
        IssueResponse? issue)
    {
        Outcome = outcome;
        Issue = issue;
    }

    public IssueMutationOutcome Outcome { get; }

    public IssueResponse? Issue { get; }

    public static IssueMutationResult Success(IssueResponse issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return new IssueMutationResult(IssueMutationOutcome.Succeeded, issue);
    }

    public static IssueMutationResult Failure(IssueMutationOutcome outcome)
    {
        if (outcome == IssueMutationOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when the Issue mutation succeeds.");
        }

        return new IssueMutationResult(outcome, issue: null);
    }
}

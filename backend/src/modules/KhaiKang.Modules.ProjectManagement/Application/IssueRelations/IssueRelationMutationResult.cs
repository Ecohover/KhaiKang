using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record IssueRelationMutationResult
{
    private IssueRelationMutationResult(
        IssueRelationMutationOutcome outcome,
        IssueRelationResponse? relation)
    {
        Outcome = outcome;
        Relation = relation;
    }

    public IssueRelationMutationOutcome Outcome { get; }

    public IssueRelationResponse? Relation { get; }

    public static IssueRelationMutationResult Created(IssueRelationResponse relation)
    {
        ArgumentNullException.ThrowIfNull(relation);
        return new IssueRelationMutationResult(
            IssueRelationMutationOutcome.Succeeded,
            relation);
    }

    public static IssueRelationMutationResult Deleted()
    {
        return new IssueRelationMutationResult(
            IssueRelationMutationOutcome.Succeeded,
            relation: null);
    }

    public static IssueRelationMutationResult Failure(IssueRelationMutationOutcome outcome)
    {
        if (outcome == IssueRelationMutationOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Created or Deleted when the Issue relation mutation succeeds.");
        }

        return new IssueRelationMutationResult(outcome, relation: null);
    }
}

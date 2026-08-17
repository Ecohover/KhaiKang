using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record ProjectMemberMutationResult
{
    private ProjectMemberMutationResult(
        ProjectMemberMutationOutcome outcome,
        ProjectMemberResponse? member)
    {
        Outcome = outcome;
        Member = member;
    }

    public ProjectMemberMutationOutcome Outcome { get; }

    public ProjectMemberResponse? Member { get; }

    public static ProjectMemberMutationResult Success(ProjectMemberResponse member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return new ProjectMemberMutationResult(
            ProjectMemberMutationOutcome.Succeeded,
            member);
    }

    public static ProjectMemberMutationResult Removed()
    {
        return new ProjectMemberMutationResult(
            ProjectMemberMutationOutcome.Succeeded,
            member: null);
    }

    public static ProjectMemberMutationResult Failure(ProjectMemberMutationOutcome outcome)
    {
        if (outcome == ProjectMemberMutationOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success or Removed when the member mutation succeeds.");
        }

        return new ProjectMemberMutationResult(outcome, member: null);
    }
}

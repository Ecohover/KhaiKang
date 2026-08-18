using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record UpdateProjectMemberRolesResult
{
    private UpdateProjectMemberRolesResult(
        UpdateProjectMemberRolesOutcome outcome,
        ProjectMemberResponse? member)
    {
        Outcome = outcome;
        Member = member;
    }

    public UpdateProjectMemberRolesOutcome Outcome { get; }

    public ProjectMemberResponse? Member { get; }

    public static UpdateProjectMemberRolesResult Success(ProjectMemberResponse member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return new UpdateProjectMemberRolesResult(
            UpdateProjectMemberRolesOutcome.Succeeded,
            member);
    }

    public static UpdateProjectMemberRolesResult Failure(
        UpdateProjectMemberRolesOutcome outcome)
    {
        if (outcome == UpdateProjectMemberRolesOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when updating project member roles succeeds.");
        }

        return new UpdateProjectMemberRolesResult(outcome, member: null);
    }
}

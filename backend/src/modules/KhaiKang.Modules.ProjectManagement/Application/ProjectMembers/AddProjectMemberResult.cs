using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record AddProjectMemberResult
{
    private AddProjectMemberResult(
        AddProjectMemberOutcome outcome,
        ProjectMemberResponse? member)
    {
        Outcome = outcome;
        Member = member;
    }

    public AddProjectMemberOutcome Outcome { get; }

    public ProjectMemberResponse? Member { get; }

    public static AddProjectMemberResult Success(ProjectMemberResponse member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return new AddProjectMemberResult(AddProjectMemberOutcome.Succeeded, member);
    }

    public static AddProjectMemberResult Failure(AddProjectMemberOutcome outcome)
    {
        if (outcome == AddProjectMemberOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when adding a project member succeeds.");
        }

        return new AddProjectMemberResult(outcome, member: null);
    }
}

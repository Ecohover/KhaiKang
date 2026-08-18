using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record UpdateProjectResult
{
    private UpdateProjectResult(
        UpdateProjectOutcome outcome,
        ProjectResponse? project)
    {
        Outcome = outcome;
        Project = project;
    }

    public UpdateProjectOutcome Outcome { get; }

    public ProjectResponse? Project { get; }

    public static UpdateProjectResult Success(ProjectResponse project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new UpdateProjectResult(UpdateProjectOutcome.Succeeded, project);
    }

    public static UpdateProjectResult Failure(UpdateProjectOutcome outcome)
    {
        if (outcome == UpdateProjectOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when the project update succeeds.");
        }

        return new UpdateProjectResult(outcome, project: null);
    }
}

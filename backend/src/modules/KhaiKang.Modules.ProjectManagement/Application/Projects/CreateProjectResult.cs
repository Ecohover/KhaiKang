using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record CreateProjectResult
{
    private CreateProjectResult(
        CreateProjectOutcome outcome,
        ProjectResponse? project)
    {
        Outcome = outcome;
        Project = project;
    }

    public CreateProjectOutcome Outcome { get; }

    public ProjectResponse? Project { get; }

    public static CreateProjectResult Success(ProjectResponse project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new CreateProjectResult(CreateProjectOutcome.Succeeded, project);
    }

    public static CreateProjectResult Failure(CreateProjectOutcome outcome)
    {
        if (outcome == CreateProjectOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when project creation succeeds.");
        }

        return new CreateProjectResult(outcome, project: null);
    }
}

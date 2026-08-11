namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectDetailsChange
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required ProjectStatus Status { get; init; }
}

namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectCreation
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}

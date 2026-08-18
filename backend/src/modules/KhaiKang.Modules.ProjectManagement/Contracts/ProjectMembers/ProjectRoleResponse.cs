namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectRoleResponse
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}

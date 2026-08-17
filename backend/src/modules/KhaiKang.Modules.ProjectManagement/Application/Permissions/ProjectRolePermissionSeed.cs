namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record ProjectRolePermissionSeed
{
    public required Guid Id { get; init; }

    public required Guid ProjectRoleId { get; init; }

    public required Guid PermissionId { get; init; }
}

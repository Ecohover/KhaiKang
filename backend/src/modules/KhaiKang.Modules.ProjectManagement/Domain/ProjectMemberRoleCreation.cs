namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMemberRoleCreation
{
    public required Guid Id { get; init; }

    public required Guid ProjectMemberId { get; init; }

    public required Guid ProjectRoleId { get; init; }
}

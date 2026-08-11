namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMemberCreation
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required Guid AccountId { get; init; }
}

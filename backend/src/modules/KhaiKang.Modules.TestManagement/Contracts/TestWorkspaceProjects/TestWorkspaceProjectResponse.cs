namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestWorkspaceProjectResponse
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset LinkedAt { get; init; }

    public required int Version { get; init; }
}

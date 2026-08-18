namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestWorkspaceMemberResponse
{
    public required Guid Id { get; init; }

    public required Guid AccountId { get; init; }

    public required string Username { get; init; }

    public required string Role { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset JoinedAt { get; init; }

    public required int Version { get; init; }
}

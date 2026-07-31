namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectMemberResponse(
    Guid Id,
    Guid AccountId,
    string Username,
    string Status,
    IReadOnlyList<string> RoleCodes,
    DateTimeOffset JoinedAt,
    int Version);

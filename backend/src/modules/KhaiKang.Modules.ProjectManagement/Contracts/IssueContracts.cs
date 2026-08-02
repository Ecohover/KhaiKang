namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueOptionResponse(
    string Code,
    string Name,
    string? Description,
    string? Category = null);

public sealed record IssueMetadataResponse(
    IReadOnlyList<IssueOptionResponse> Types,
    IReadOnlyList<IssueOptionResponse> Statuses,
    IReadOnlyList<IssueOptionResponse> Priorities);

public sealed record CreateIssueRequest(
    string Title,
    string TypeCode,
    string? PriorityCode,
    string? Description,
    string? UserStory,
    string? DefinitionOfDone,
    Guid? AssigneeAccountId);

public sealed record UpdateIssueStatusRequest(string StatusCode, int Version);

public sealed record UpdateIssueRequest(
    string Title,
    string TypeCode,
    string PriorityCode,
    string? Description,
    string? UserStory,
    string? DefinitionOfDone,
    string? CompletionSummary,
    int Version);

public sealed record UpdateIssueAssigneeRequest(Guid? AssigneeAccountId, int Version);

public sealed record IssueResponse(
    Guid Id,
    Guid ProjectId,
    int IssueNo,
    string Key,
    string Title,
    string? Description,
    string? UserStory,
    string? DefinitionOfDone,
    string? CompletionSummary,
    string TypeCode,
    string TypeName,
    string StatusCode,
    string StatusName,
    string PriorityCode,
    string PriorityName,
    Guid ReporterAccountId,
    string ReporterUsername,
    Guid? AssigneeAccountId,
    string? AssigneeUsername,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

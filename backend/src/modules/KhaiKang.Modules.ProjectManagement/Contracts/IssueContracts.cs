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

public sealed class IssueListQuery
{
    public string? Search { get; init; }

    public string? TypeCode { get; init; }

    public string? StatusCode { get; init; }

    public string? PriorityCode { get; init; }

    public Guid? AssigneeAccountId { get; init; }

    public bool? Unassigned { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed record UpdateIssueStatusRequest(string StatusCode, int Version);

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

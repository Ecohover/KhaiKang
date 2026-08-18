namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueResponse
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required int IssueNo { get; init; }

    public required string Key { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? UserStory { get; init; }

    public string? DefinitionOfDone { get; init; }

    public string? CompletionSummary { get; init; }

    public required string TypeCode { get; init; }

    public required string TypeName { get; init; }

    public required string StatusCode { get; init; }

    public required string StatusName { get; init; }

    public required string PriorityCode { get; init; }

    public required string PriorityName { get; init; }

    public required Guid ReporterAccountId { get; init; }

    public required string ReporterUsername { get; init; }

    public Guid? AssigneeAccountId { get; init; }

    public string? AssigneeUsername { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }
}

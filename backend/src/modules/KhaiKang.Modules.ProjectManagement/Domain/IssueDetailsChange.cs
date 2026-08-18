namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueDetailsChange
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? UserStory { get; init; }

    public string? DefinitionOfDone { get; init; }

    public string? CompletionSummary { get; init; }

    public required Guid IssueTypeId { get; init; }

    public required Guid IssuePriorityId { get; init; }
}

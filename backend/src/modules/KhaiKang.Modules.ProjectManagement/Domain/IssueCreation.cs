namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueCreation
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required int IssueNo { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? UserStory { get; init; }

    public string? DefinitionOfDone { get; init; }

    public required Guid IssueTypeId { get; init; }

    public required Guid IssueStatusId { get; init; }

    public required Guid IssuePriorityId { get; init; }

    public Guid? AssigneeAccountId { get; init; }
}

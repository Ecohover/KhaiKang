namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class Issue
{
    private Issue() { }

    public Issue(
        Guid id,
        Guid projectId,
        int issueNo,
        string title,
        string? description,
        string? userStory,
        string? definitionOfDone,
        Guid issueTypeId,
        Guid issueStatusId,
        Guid issuePriorityId,
        Guid reporterAccountId,
        Guid? assigneeAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        IssueNo = issueNo;
        Title = title;
        Description = description;
        UserStory = userStory;
        DefinitionOfDone = definitionOfDone;
        IssueTypeId = issueTypeId;
        IssueStatusId = issueStatusId;
        IssuePriorityId = issuePriorityId;
        ReporterAccountId = reporterAccountId;
        AssigneeAccountId = assigneeAccountId;
        CreatedAt = createdAt;
        CreatedByAccountId = reporterAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = reporterAccountId;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public int IssueNo { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? UserStory { get; private set; }
    public string? DefinitionOfDone { get; private set; }
    public Guid IssueTypeId { get; private set; }
    public IssueType IssueType { get; private set; } = null!;
    public Guid IssueStatusId { get; private set; }
    public IssueStatus IssueStatus { get; private set; } = null!;
    public Guid IssuePriorityId { get; private set; }
    public IssuePriority IssuePriority { get; private set; } = null!;
    public Guid ReporterAccountId { get; private set; }
    public Guid? AssigneeAccountId { get; private set; }
    public string? CompletionSummary { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;

    public void ChangeStatus(
        Guid statusId,
        string statusCode,
        Guid actorAccountId,
        DateTimeOffset occurredAt)
    {
        IssueStatusId = statusId;
        CompletedAt = statusCode == "completed" ? occurredAt : null;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }

    public void UpdateDetails(
        string title,
        string? description,
        string? userStory,
        string? definitionOfDone,
        string? completionSummary,
        Guid issueTypeId,
        Guid issuePriorityId,
        Guid actorAccountId,
        DateTimeOffset occurredAt)
    {
        Title = title;
        Description = description;
        UserStory = userStory;
        DefinitionOfDone = definitionOfDone;
        CompletionSummary = completionSummary;
        IssueTypeId = issueTypeId;
        IssuePriorityId = issuePriorityId;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }

    public void ChangeAssignee(
        Guid? assigneeAccountId,
        Guid actorAccountId,
        DateTimeOffset occurredAt)
    {
        AssigneeAccountId = assigneeAccountId;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }
}

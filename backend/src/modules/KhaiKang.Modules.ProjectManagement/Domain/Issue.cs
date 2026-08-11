namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class Issue : AuditableEntity
{
    private Issue() { }

    private Issue(IssueCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        ProjectId = creation.ProjectId;
        IssueNo = creation.IssueNo;
        Title = creation.Title;
        Description = creation.Description;
        UserStory = creation.UserStory;
        DefinitionOfDone = creation.DefinitionOfDone;
        IssueTypeId = creation.IssueTypeId;
        IssueStatusId = creation.IssueStatusId;
        IssuePriorityId = creation.IssuePriorityId;
        ReporterAccountId = context.ActorAccountId;
        AssigneeAccountId = creation.AssigneeAccountId;
        InitializeAudit(context);
    }

    public static Issue Create(IssueCreation creation, ChangeContext context)
    {
        return new Issue(creation, context);
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
    public void ChangeStatus(
        Guid statusId,
        IssueStatusCategory category,
        ChangeContext context)
    {
        IssueStatusId = statusId;
        CompletedAt = category == IssueStatusCategory.Done ? context.OccurredAt : null;
        RecordChange(context);
    }

    public void UpdateDetails(IssueDetailsChange change, ChangeContext context)
    {
        Title = change.Title;
        Description = change.Description;
        UserStory = change.UserStory;
        DefinitionOfDone = change.DefinitionOfDone;
        CompletionSummary = change.CompletionSummary;
        IssueTypeId = change.IssueTypeId;
        IssuePriorityId = change.IssuePriorityId;
        RecordChange(context);
    }

    public void ChangeAssignee(Guid? assigneeAccountId, ChangeContext context)
    {
        AssigneeAccountId = assigneeAccountId;
        RecordChange(context);
    }
}

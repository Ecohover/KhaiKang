namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectAuditEvent
{
    private const string HumanActorType = "human";
    private const string SucceededOutcome = "succeeded";
    private const string ProjectCreatedEventType = "project_created";
    private const string ProjectUpdatedEventType = "project_updated";
    private const string ProjectMemberAddedEventType = "project_member_added";
    private const string ProjectMemberRolesChangedEventType = "project_member_roles_changed";
    private const string ProjectMemberRemovedEventType = "project_member_removed";
    private const string IssueCreatedEventType = "issue_created";
    private const string IssueStatusChangedEventType = "issue_status_changed";
    private const string IssueUpdatedEventType = "issue_updated";
    private const string IssueAssigneeChangedEventType = "issue_assignee_changed";
    private const string IssueRelationCreatedEventType = "issue_relation_created";
    private const string IssueRelationDeletedEventType = "issue_relation_deleted";

    private ProjectAuditEvent()
    {
    }

    private ProjectAuditEvent(string eventType, Guid targetId, ChangeContext context)
    {
        Id = Guid.NewGuid();
        ActorId = context.ActorAccountId;
        EventType = eventType;
        OccurredAt = context.OccurredAt;
        TargetId = targetId;
    }

    public static ProjectAuditEvent ProjectCreated(Guid targetId, ChangeContext context) =>
        Create(ProjectCreatedEventType, targetId, context);

    public static ProjectAuditEvent ProjectUpdated(Guid targetId, ChangeContext context) =>
        Create(ProjectUpdatedEventType, targetId, context);

    public static ProjectAuditEvent ProjectMemberAdded(Guid targetId, ChangeContext context) =>
        Create(ProjectMemberAddedEventType, targetId, context);

    public static ProjectAuditEvent ProjectMemberRolesChanged(Guid targetId, ChangeContext context) =>
        Create(ProjectMemberRolesChangedEventType, targetId, context);

    public static ProjectAuditEvent ProjectMemberRemoved(Guid targetId, ChangeContext context) =>
        Create(ProjectMemberRemovedEventType, targetId, context);

    public static ProjectAuditEvent IssueCreated(Guid targetId, ChangeContext context) =>
        Create(IssueCreatedEventType, targetId, context);

    public static ProjectAuditEvent IssueStatusChanged(Guid targetId, ChangeContext context) =>
        Create(IssueStatusChangedEventType, targetId, context);

    public static ProjectAuditEvent IssueUpdated(Guid targetId, ChangeContext context) =>
        Create(IssueUpdatedEventType, targetId, context);

    public static ProjectAuditEvent IssueAssigneeChanged(Guid targetId, ChangeContext context) =>
        Create(IssueAssigneeChangedEventType, targetId, context);

    public static ProjectAuditEvent IssueRelationCreated(Guid targetId, ChangeContext context) =>
        Create(IssueRelationCreatedEventType, targetId, context);

    public static ProjectAuditEvent IssueRelationDeleted(Guid targetId, ChangeContext context) =>
        Create(IssueRelationDeletedEventType, targetId, context);

    public Guid Id { get; private set; }

    public Guid ActorId { get; private set; }

    public string ActorType { get; private set; } = HumanActorType;

    public string EventType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid TargetId { get; private set; }

    public string Outcome { get; private set; } = SucceededOutcome;

    private static ProjectAuditEvent Create(
        string eventType,
        Guid targetId,
        ChangeContext context)
    {
        return new ProjectAuditEvent(eventType, targetId, context);
    }
}
